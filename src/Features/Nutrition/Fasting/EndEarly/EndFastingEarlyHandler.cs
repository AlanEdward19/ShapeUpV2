using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ShapeUp.Features.Nutrition.Fasting.Shared;
using ShapeUp.Features.Nutrition.Fasting.Shared.ViewModels;
using ShapeUp.Features.Nutrition.Infrastructure.Data;
using ShapeUp.Features.Nutrition.Shared.Entities;
using ShapeUp.Features.Nutrition.Shared.Errors;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.Fasting.EndEarly;

/// <summary>
/// Ends the fasting window in progress, whether it comes from the saved agenda or from a manual override, and records it
/// in the history as EndedEarly. An agenda window has no override row yet, so one is created in the eating phase to carry
/// the record and move the clock.
/// </summary>
public sealed class EndFastingEarlyHandler(
    NutritionDbContext dbContext,
    FastingStateReader stateReader,
    FastingClockCalculator clockCalculator,
    IFastingProfessionalNotifier notifier,
    IValidator<EndFastingEarlyCommand> validator)
{
    public async Task<Result<FastingSnapshotResponse>> HandleAsync(
        EndFastingEarlyCommand command,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<FastingSnapshotResponse>.Failure(
                CommonErrors.Validation(string.Join("; ", validation.Errors.Select(x => x.ErrorMessage))));

        var state = await stateReader.LoadAsync(actorUserId, cancellationToken);
        if (state.Clock.Status != FastingClockCalculator.StatusFasting)
            return Result<FastingSnapshotResponse>.Failure(NutritionErrors.FastingNoActiveFast());

        var reason = FastingMapper.NormalizeReason(command.Reason);
        var now = state.UtcNow;

        if (state.ActiveOverride is not null)
        {
            state.ActiveOverride.EndEarly(now, reason);
        }
        else
        {
            var agenda = state.Agenda!;
            var window = clockCalculator.CurrentFastWindowFromAgenda(
                agenda.EatHours!.Value, agenda.EatingStartMinutes!.Value, agenda.TimeZone!, now);
            if (window is null)
                return Result<FastingSnapshotResponse>.Failure(NutritionErrors.FastingNoActiveFast());

            var record = new FastingOverride
            {
                Id = Guid.NewGuid(),
                UserId = actorUserId,
                Protocol = agenda.Protocol!,
                FastHours = agenda.FastHours!.Value,
                EatHours = agenda.EatHours!.Value,
                StartedAtUtc = window.Value.StartUtc,
                FastEndsAtUtc = window.Value.EndUtc
            };
            record.EndEarly(now, reason);
            dbContext.FastingOverrides.Add(record);
            state = state with { ActiveOverride = record, ClockSource = FastingMapper.ClockSourceOverride };
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // A concurrent request already created the active override for this user.
            return Result<FastingSnapshotResponse>.Failure(NutritionErrors.FastingNoActiveFast());
        }

        await notifier.NotifyEndedEarlyAsync(actorUserId, state.Agenda?.RecommendedByUserId, reason, cancellationToken);

        return Result<FastingSnapshotResponse>.Success(await stateReader.SnapshotAsync(actorUserId, cancellationToken));
    }
}
