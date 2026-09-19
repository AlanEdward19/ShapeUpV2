using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ShapeUp.Features.Nutrition.Fasting.Shared;
using ShapeUp.Features.Nutrition.Fasting.Shared.ViewModels;
using ShapeUp.Features.Nutrition.Infrastructure.Data;
using ShapeUp.Features.Nutrition.Shared.Entities;
using ShapeUp.Features.Relationships.Shared.Abstractions;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.Fasting.SetRecommendation;

public sealed class SetFastingRecommendationHandler(
    NutritionDbContext dbContext,
    IUtcClock utcClock,
    IProfessionalClientRelationshipRepository relationshipRepository,
    IValidator<SetFastingRecommendationCommand> validator)
{
    public async Task<Result<FastingRecommendationDto>> HandleAsync(
        SetFastingRecommendationCommand command,
        int professionalUserId,
        int clientUserId,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<FastingRecommendationDto>.Failure(
                CommonErrors.Validation(string.Join("; ", validation.Errors.Select(x => x.ErrorMessage))));

        var relationship = await relationshipRepository.GetActiveAsync(
            professionalUserId,
            clientUserId,
            "Training",
            cancellationToken);

        if (relationship is null)
            return Result<FastingRecommendationDto>.Failure(
                CommonErrors.Forbidden("You are not allowed to set a fasting recommendation for this client."));

        var hours = FastingClockCalculator.ParseProtocol(command.Protocol, customFastHours: null);
        if (hours is null)
            return Result<FastingRecommendationDto>.Failure(CommonErrors.Validation("Protocol is invalid."));

        var (fastHours, _) = hours.Value;
        var nowUtc = utcClock.UtcNow;

        var agenda = await dbContext.FastingAgendas.FirstOrDefaultAsync(a => a.UserId == clientUserId, cancellationToken);
        if (agenda is null)
        {
            agenda = new FastingAgenda { UserId = clientUserId };
            dbContext.FastingAgendas.Add(agenda);
        }

        agenda.RecommendedProtocol = command.Protocol;
        agenda.RecommendedFastHours = fastHours;
        agenda.UpdatedAtUtc = nowUtc;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<FastingRecommendationDto>.Success(
            new FastingRecommendationDto(command.Protocol, fastHours));
    }
}
