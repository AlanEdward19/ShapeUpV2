using Microsoft.EntityFrameworkCore;
using ShapeUp.Features.Authorization.ProfessionalCapabilities;
using ShapeUp.Features.Authorization.Shared.Abstractions;
using ShapeUp.Features.Nutrition.Clients.Shared;
using ShapeUp.Features.Nutrition.Diary.Shared;
using ShapeUp.Features.Nutrition.Infrastructure.Data;
using ShapeUp.Features.Nutrition.Infrastructure.Policies;
using ShapeUp.Features.Relationships.Shared.Abstractions;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.Clients.ClientsAdherence;

public class GetClientsAdherenceHandler(
    IProfessionalCapabilityService capabilityService,
    IProfessionalClientRelationshipRepository relationshipRepository,
    IUserRepository userRepository,
    NutritionDbContext dbContext)
{
    public const int DefaultDays = 7;
    public const int MaxDays = 90;

    public async Task<Result<NutritionClientAdherenceResponse[]>> HandleAsync(
        int? days,
        int nutritionistUserId,
        CancellationToken cancellationToken,
        DateOnly? today = null)
    {
        var capabilities = await capabilityService.GetAsync(nutritionistUserId, cancellationToken);
        if (!capabilities.Nutrition)
            return Result<NutritionClientAdherenceResponse[]>.Failure(CommonErrors.Forbidden("Nutrition capability is required."));

        var window = days ?? DefaultDays;
        if (window is < 1 or > MaxDays)
            return Result<NutritionClientAdherenceResponse[]>.Failure(CommonErrors.Validation($"'days' must be between 1 and {MaxDays}."));

        var relationships = await relationshipRepository.ListActiveByProfessionalAsync(
            nutritionistUserId, NutritionAccessPolicy.RelationshipType, cancellationToken);
        var clientIds = relationships.Select(r => r.ClientUserId).ToList();

        var end = today ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var start = end.AddDays(-(window - 1));

        var windowDays = await dbContext.DiaryDays
            .AsNoTracking()
            .Include(d => d.Entries)
            .Where(d => clientIds.Contains(d.UserId) && d.Date >= start && d.Date <= end)
            .ToListAsync(cancellationToken);

        var lastLogged = (await dbContext.DiaryDays
                .AsNoTracking()
                .Where(d => clientIds.Contains(d.UserId) && d.Entries.Any())
                .GroupBy(d => d.UserId)
                .Select(g => new { UserId = g.Key, Last = g.Max(d => d.Date) })
                .ToListAsync(cancellationToken))
            .ToDictionary(x => x.UserId, x => x.Last);

        var goals = await dbContext.Profiles
            .AsNoTracking()
            .Where(p => clientIds.Contains(p.UserId))
            .ToDictionaryAsync(p => p.UserId, p => p.ActiveGoal, cancellationToken);

        var response = new List<NutritionClientAdherenceResponse>();
        foreach (var relationship in relationships)
        {
            var clientId = relationship.ClientUserId;
            goals.TryGetValue(clientId, out var goal);
            var adherence = NutritionAdherenceCalculator.Calculate(windowDays.Where(d => d.UserId == clientId), goal);
            var user = await userRepository.GetByIdAsync(clientId, cancellationToken);

            response.Add(new NutritionClientAdherenceResponse(
                clientId,
                user?.DisplayName,
                window,
                adherence.DaysLogged,
                adherence.DaysWithinGoal,
                goal is null ? null : DiaryMapper.ToTotalsDto(goal),
                adherence.AverageConsumed is null ? null : DiaryMapper.ToTotalsDto(adherence.AverageConsumed),
                lastLogged.TryGetValue(clientId, out var last) ? last : null));
        }

        return Result<NutritionClientAdherenceResponse[]>.Success(response.ToArray());
    }
}
