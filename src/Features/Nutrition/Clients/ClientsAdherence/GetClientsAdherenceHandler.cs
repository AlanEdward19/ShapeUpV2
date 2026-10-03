using Microsoft.EntityFrameworkCore;
using ShapeUp.Features.Authorization.ProfessionalCapabilities;
using ShapeUp.Features.Authorization.Shared.Abstractions;
using ShapeUp.Features.Nutrition.Clients.Shared;
using ShapeUp.Features.Nutrition.Diary.Shared;
using ShapeUp.Features.Nutrition.Infrastructure.Data;
using ShapeUp.Features.Nutrition.Infrastructure.Policies;
using ShapeUp.Features.Relationships.Shared.Abstractions;
using ShapeUp.Shared.Pagination;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.Clients.ClientsAdherence;

/// <summary>
/// Adherence of the nutritionist's clients, one page at a time (keyset on the relationship Id).
/// "Today" is the current date in each client's time zone when the client has one saved (the fasting agenda's
/// <c>TimeZone</c>), UTC otherwise. The window is capped at <see cref="MaxDays"/> days.
/// </summary>
public class GetClientsAdherenceHandler(
    IProfessionalCapabilityService capabilityService,
    IProfessionalClientRelationshipRepository relationshipRepository,
    IUserRepository userRepository,
    NutritionDbContext dbContext)
{
    public const int DefaultDays = 7;
    public const int MaxDays = 90;

    public async Task<Result<KeysetPageResponse<NutritionClientAdherenceResponse>>> HandleAsync(
        int? days,
        string? cursor,
        int? pageSize,
        int nutritionistUserId,
        CancellationToken cancellationToken,
        DateOnly? today = null)
    {
        var capabilities = await capabilityService.GetAsync(nutritionistUserId, cancellationToken);
        if (!capabilities.Nutrition)
            return Result<KeysetPageResponse<NutritionClientAdherenceResponse>>.Failure(CommonErrors.Forbidden("Nutrition capability is required."));

        var window = days ?? DefaultDays;
        if (window is < 1 or > MaxDays)
            return Result<KeysetPageResponse<NutritionClientAdherenceResponse>>.Failure(CommonErrors.Validation($"'days' must be between 1 and {MaxDays}."));

        int? afterId = null;
        if (!string.IsNullOrWhiteSpace(cursor))
        {
            if (!KeysetCursorCodec.TryDecodeLong(cursor, out var decoded) || decoded is < 0 or > int.MaxValue)
                return Result<KeysetPageResponse<NutritionClientAdherenceResponse>>.Failure(CommonErrors.Validation("Invalid cursor."));
            afterId = (int)decoded;
        }

        var size = new KeysetPageRequest(cursor, pageSize).NormalizePageSize();
        var fetched = await relationshipRepository.ListActiveByProfessionalKeysetAsync(
            nutritionistUserId, NutritionAccessPolicy.RelationshipType, afterId, size + 1, cancellationToken);
        var hasMore = fetched.Count > size;
        var relationships = fetched.Take(size).ToList();

        if (relationships.Count == 0)
            return Result<KeysetPageResponse<NutritionClientAdherenceResponse>>.Success(
                new KeysetPageResponse<NutritionClientAdherenceResponse>([], null));

        var clientIds = relationships.Select(r => r.ClientUserId).Distinct().ToList();

        // Each client's "today" in their own zone (UTC when none is saved).
        var utcToday = today ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var zones = await dbContext.FastingAgendas
            .AsNoTracking()
            .Where(a => clientIds.Contains(a.UserId) && a.TimeZone != null)
            .ToDictionaryAsync(a => a.UserId, a => a.TimeZone, cancellationToken);
        var ends = clientIds.ToDictionary(id => id, id => today ?? LocalToday(zones.GetValueOrDefault(id), utcToday));

        // One query for the whole page: the widest window, narrowed per client in memory.
        var from = ends.Values.Min().AddDays(-(window - 1));
        var to = ends.Values.Max();
        var windowDays = (await dbContext.DiaryDays
                .AsNoTracking()
                .Include(d => d.Entries)
                .Where(d => clientIds.Contains(d.UserId) && d.Date >= from && d.Date <= to)
                .ToListAsync(cancellationToken))
            .ToLookup(d => d.UserId);

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

        var names = (await userRepository.GetByIdsAsync(clientIds, cancellationToken))
            .ToDictionary(u => u.Id, u => u.DisplayName);

        var items = relationships.Select(relationship =>
        {
            var clientId = relationship.ClientUserId;
            goals.TryGetValue(clientId, out var goal);
            var end = ends[clientId];
            var start = end.AddDays(-(window - 1));
            var adherence = NutritionAdherenceCalculator.Calculate(
                windowDays[clientId].Where(d => d.Date >= start && d.Date <= end), goal);

            return new NutritionClientAdherenceResponse(
                clientId,
                names.GetValueOrDefault(clientId),
                window,
                adherence.DaysLogged,
                adherence.DaysWithinGoal,
                goal is null ? null : DiaryMapper.ToTotalsDto(goal),
                adherence.AverageConsumed is null ? null : DiaryMapper.ToTotalsDto(adherence.AverageConsumed),
                lastLogged.TryGetValue(clientId, out var last) ? last : null);
        }).ToArray();

        var nextCursor = hasMore ? KeysetCursorCodec.EncodeLong(relationships[^1].Id) : null;
        return Result<KeysetPageResponse<NutritionClientAdherenceResponse>>.Success(
            new KeysetPageResponse<NutritionClientAdherenceResponse>(items, nextCursor));
    }

    private static DateOnly LocalToday(string? timeZoneId, DateOnly utcToday)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
            return utcToday;

        try
        {
            var zone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone));
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return utcToday;
        }
    }
}
