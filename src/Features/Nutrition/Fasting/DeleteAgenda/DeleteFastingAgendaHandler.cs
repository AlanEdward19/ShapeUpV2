using Microsoft.EntityFrameworkCore;
using ShapeUp.Features.Nutrition.Fasting.Shared;
using ShapeUp.Features.Nutrition.Fasting.Shared.ViewModels;
using ShapeUp.Features.Nutrition.Infrastructure.Data;
using ShapeUp.Features.Nutrition.Shared.Entities;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.Fasting.DeleteAgenda;

/// <summary>
/// Turns fasting off: clears the saved agenda and cancels any active override. History is kept and the recommendation is
/// not touched. Repeating the call is a no-op that returns the same (idle) snapshot.
/// </summary>
public sealed class DeleteFastingAgendaHandler(
    NutritionDbContext dbContext,
    FastingStateReader stateReader,
    IUtcClock utcClock)
{
    public async Task<Result<FastingSnapshotResponse>> HandleAsync(int actorUserId, CancellationToken cancellationToken)
    {
        var now = utcClock.UtcNow;

        var agenda = await dbContext.FastingAgendas.FirstOrDefaultAsync(a => a.UserId == actorUserId, cancellationToken);
        if (agenda is not null)
        {
            if (agenda.RecommendedProtocol is null)
            {
                dbContext.FastingAgendas.Remove(agenda);
            }
            else
            {
                // Keep the status a saved agenda implied before it is cleared.
                agenda.RecommendationStatus = FastingMapper.EffectiveRecommendationStatus(agenda);
                agenda.FastHours = null;
                agenda.EatHours = null;
                agenda.Protocol = null;
                agenda.EatingStartMinutes = null;
                agenda.TimeZone = null;
                agenda.UpdatedAtUtc = now;
            }
        }

        var active = await dbContext.FastingOverrides
            .Where(o => o.UserId == actorUserId
                && (o.Status == FastingOverride.StatusFasting || o.Status == FastingOverride.StatusEating))
            .ToListAsync(cancellationToken);
        foreach (var activeOverride in active)
        {
            activeOverride.Status = FastingOverride.StatusCancelled;
            activeOverride.CompletedAtUtc = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<FastingSnapshotResponse>.Success(await stateReader.SnapshotAsync(actorUserId, cancellationToken));
    }
}
