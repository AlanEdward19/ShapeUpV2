using Microsoft.EntityFrameworkCore;
using ShapeUp.Features.Nutrition.Fasting.Shared.ViewModels;
using ShapeUp.Features.Nutrition.Infrastructure.Data;
using ShapeUp.Features.Nutrition.Shared.Entities;

namespace ShapeUp.Features.Nutrition.Fasting.Shared;

/// <summary>The resolved fasting state of a user at <see cref="UtcNow"/>; the entities are tracked so handlers can change them.</summary>
public sealed record FastingState(
    FastingAgenda? Agenda,
    FastingOverride? ActiveOverride,
    FastingClockState Clock,
    string? ClockSource,
    DateTime UtcNow);

/// <summary>Single place that resolves the clock (override over agenda) and builds the snapshot every fasting screen reads.</summary>
public sealed class FastingStateReader(
    NutritionDbContext dbContext,
    IUtcClock utcClock,
    FastingClockCalculator clockCalculator)
{
    public async Task<FastingState> LoadAsync(int userId, CancellationToken cancellationToken)
    {
        var utcNow = utcClock.UtcNow;
        var agenda = await dbContext.FastingAgendas
            .FirstOrDefaultAsync(a => a.UserId == userId, cancellationToken);

        var activeOverride = await dbContext.FastingOverrides
            .FirstOrDefaultAsync(
                o => o.UserId == userId
                    && (o.Status == FastingOverride.StatusFasting || o.Status == FastingOverride.StatusEating),
                cancellationToken);

        if (activeOverride is not null
            && activeOverride.EatEndsAtUtc is not null
            && activeOverride.EatEndsAtUtc.Value <= utcNow)
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
                activeOverride.Status, activeOverride.FastEndsAtUtc, activeOverride.EatEndsAtUtc, utcNow);
            clockSource = FastingMapper.ClockSourceOverride;
        }
        else if (FastingMapper.HasSavedAgenda(agenda))
        {
            clockState = clockCalculator.FromAgenda(
                agenda!.FastHours!.Value, agenda.EatHours!.Value, agenda.EatingStartMinutes!.Value, agenda.TimeZone!, utcNow);
            clockSource = FastingMapper.ClockSourceAgenda;
        }
        else
        {
            clockState = FastingClockCalculator.Idle();
        }

        return new FastingState(agenda, activeOverride, clockState, clockSource, utcNow);
    }

    public async Task<FastingSnapshotResponse> SnapshotAsync(int userId, CancellationToken cancellationToken) =>
        ToSnapshot(await LoadAsync(userId, cancellationToken));

    public static FastingSnapshotResponse ToSnapshot(FastingState state) =>
        new(
            FastingMapper.HasSavedAgenda(state.Agenda) ? FastingMapper.ToAgendaDto(state.Agenda!) : null,
            state.ActiveOverride is null ? null : FastingMapper.ToOverrideDto(state.ActiveOverride),
            state.Agenda is null ? null : FastingMapper.ToRecommendationDto(state.Agenda),
            FastingMapper.ToClockDto(state.Clock, state.ClockSource));
}
