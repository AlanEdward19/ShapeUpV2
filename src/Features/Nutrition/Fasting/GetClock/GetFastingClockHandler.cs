using Microsoft.EntityFrameworkCore;
using ShapeUp.Features.Nutrition.Fasting.Shared;
using ShapeUp.Features.Nutrition.Fasting.Shared.ViewModels;
using ShapeUp.Features.Nutrition.Infrastructure.Data;
using ShapeUp.Features.Nutrition.Shared.Entities;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.Fasting.GetClock;

public sealed class GetFastingClockHandler(
    NutritionDbContext dbContext,
    IUtcClock utcClock,
    FastingClockCalculator clockCalculator)
{
    public async Task<Result<FastingSnapshotResponse>> HandleAsync(
        GetFastingClockQuery query,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        var utcNow = utcClock.UtcNow;
        var agenda = await dbContext.FastingAgendas.AsNoTracking()
            .FirstOrDefaultAsync(a => a.UserId == actorUserId, cancellationToken);

        var activeOverride = await dbContext.FastingOverrides
            .FirstOrDefaultAsync(
                o => o.UserId == actorUserId
                    && (o.Status == FastingOverride.StatusFasting || o.Status == FastingOverride.StatusEating),
                cancellationToken);

        if (activeOverride is not null && ShouldLazyComplete(activeOverride, utcNow))
        {
            activeOverride.Status = FastingOverride.StatusCompleted;
            activeOverride.CompletedAtUtc = utcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
            activeOverride = null;
        }

        FastingClockState clockState;
        string? clockSource = null;

        if (activeOverride is not null)
        {
            clockState = clockCalculator.FromOverride(
                activeOverride.Status,
                activeOverride.FastEndsAtUtc,
                activeOverride.EatEndsAtUtc,
                utcNow);
            clockSource = FastingMapper.ClockSourceOverride;
        }
        else if (FastingMapper.HasSavedAgenda(agenda))
        {
            clockState = clockCalculator.FromAgenda(
                agenda!.FastHours!.Value,
                agenda.EatHours!.Value,
                agenda.EatingStartMinutes!.Value,
                agenda.TimeZone!,
                utcNow);
            clockSource = FastingMapper.ClockSourceAgenda;
        }
        else
        {
            clockState = FastingClockCalculator.Idle();
        }

        var agendaDto = FastingMapper.HasSavedAgenda(agenda) ? FastingMapper.ToAgendaDto(agenda!) : null;
        var overrideDto = activeOverride is null ? null : FastingMapper.ToOverrideDto(activeOverride);
        var recommendationDto = agenda is null ? null : FastingMapper.ToRecommendationDto(agenda);

        return Result<FastingSnapshotResponse>.Success(
            new FastingSnapshotResponse(
                agendaDto,
                overrideDto,
                recommendationDto,
                FastingMapper.ToClockDto(clockState, clockSource)));
    }

    private static bool ShouldLazyComplete(FastingOverride activeOverride, DateTime utcNow) =>
        activeOverride.EatEndsAtUtc is not null && activeOverride.EatEndsAtUtc.Value <= utcNow;
}
