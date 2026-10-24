using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ShapeUp.Features.Nutrition.Fasting.Shared;
using ShapeUp.Features.Nutrition.Fasting.Shared.ViewModels;
using ShapeUp.Features.Nutrition.Infrastructure.Data;
using ShapeUp.Features.Nutrition.Shared.Entities;
using ShapeUp.Features.Nutrition.Shared.Errors;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.Fasting.DeclineRecommendation;

public sealed class DeclineFastingRecommendationHandler(
    NutritionDbContext dbContext,
    FastingStateReader stateReader,
    IUtcClock utcClock,
    IFastingProfessionalNotifier notifier,
    IValidator<DeclineFastingRecommendationCommand> validator)
{
    public async Task<Result<FastingSnapshotResponse>> HandleAsync(
        DeclineFastingRecommendationCommand command,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<FastingSnapshotResponse>.Failure(
                CommonErrors.Validation(string.Join("; ", validation.Errors.Select(x => x.ErrorMessage))));

        var agenda = await dbContext.FastingAgendas.FirstOrDefaultAsync(a => a.UserId == actorUserId, cancellationToken);
        if (agenda?.RecommendedProtocol is null
            || FastingMapper.EffectiveRecommendationStatus(agenda) != FastingAgenda.RecommendationPending)
            return Result<FastingSnapshotResponse>.Failure(NutritionErrors.FastingNoPendingRecommendation());

        var reason = FastingMapper.NormalizeReason(command.Reason);
        var now = utcClock.UtcNow;
        agenda.RecommendationStatus = FastingAgenda.RecommendationDeclined;
        agenda.RecommendationDeclinedAtUtc = now;
        agenda.RecommendationDeclineReason = reason;
        agenda.UpdatedAtUtc = now;
        await dbContext.SaveChangesAsync(cancellationToken);

        await notifier.NotifyRecommendationDeclinedAsync(actorUserId, agenda.RecommendedByUserId, reason, cancellationToken);

        return Result<FastingSnapshotResponse>.Success(await stateReader.SnapshotAsync(actorUserId, cancellationToken));
    }
}
