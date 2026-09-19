using Microsoft.EntityFrameworkCore;
using ShapeUp.Features.Nutrition.Fasting.Shared;
using ShapeUp.Features.Nutrition.Fasting.Shared.ViewModels;
using ShapeUp.Features.Nutrition.Infrastructure.Data;
using ShapeUp.Features.Nutrition.Shared.Entities;
using ShapeUp.Features.Nutrition.Shared.Errors;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.Fasting.StartOverride;

public sealed class StartFastingOverrideHandler(
    NutritionDbContext dbContext,
    IUtcClock utcClock)
{
    public async Task<Result<FastingOverrideDto>> HandleAsync(
        StartFastingOverrideCommand command,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        var utcNow = utcClock.UtcNow;
        var agenda = await dbContext.FastingAgendas
            .FirstOrDefaultAsync(a => a.UserId == actorUserId, cancellationToken);

        if (!FastingMapper.HasSavedAgenda(agenda))
            return Result<FastingOverrideDto>.Failure(NutritionErrors.FastingAgendaRequired());

        var activeOverride = await dbContext.FastingOverrides
            .FirstOrDefaultAsync(
                o => o.UserId == actorUserId
                    && (o.Status == FastingOverride.StatusFasting || o.Status == FastingOverride.StatusEating),
                cancellationToken);

        if (activeOverride is not null)
        {
            if (ShouldLazyComplete(activeOverride, utcNow))
            {
                activeOverride.Status = FastingOverride.StatusCompleted;
                activeOverride.CompletedAtUtc = utcNow;
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            else
            {
                return Result<FastingOverrideDto>.Failure(NutritionErrors.FastingOverrideAlreadyActive());
            }
        }

        var fastHours = agenda!.FastHours!.Value;
        var eatHours = agenda.EatHours!.Value;
        var fastEndsAt = utcNow.AddHours(fastHours);
        var eatEndsAt = fastEndsAt.AddHours(eatHours);

        var created = new FastingOverride
        {
            Id = Guid.NewGuid(),
            UserId = actorUserId,
            Status = FastingOverride.StatusFasting,
            Protocol = agenda.Protocol!,
            FastHours = fastHours,
            EatHours = eatHours,
            StartedAtUtc = utcNow,
            FastEndsAtUtc = fastEndsAt,
            EatEndsAtUtc = eatEndsAt
        };

        dbContext.FastingOverrides.Add(created);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<FastingOverrideDto>.Success(FastingMapper.ToOverrideDto(created));
    }

    private static bool ShouldLazyComplete(FastingOverride activeOverride, DateTime utcNow) =>
        activeOverride.EatEndsAtUtc is not null && activeOverride.EatEndsAtUtc.Value <= utcNow;
}
