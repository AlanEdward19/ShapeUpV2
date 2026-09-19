using Microsoft.EntityFrameworkCore;
using ShapeUp.Features.Nutrition.Fasting.Shared;
using ShapeUp.Features.Nutrition.Fasting.Shared.ViewModels;
using ShapeUp.Features.Nutrition.Infrastructure.Data;
using ShapeUp.Features.Nutrition.Shared.Entities;
using ShapeUp.Features.Nutrition.Shared.Errors;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.Fasting.CancelOverride;

public sealed class CancelFastingOverrideHandler(
    NutritionDbContext dbContext,
    IUtcClock utcClock)
{
    public async Task<Result<FastingOverrideDto>> HandleAsync(
        CancelFastingOverrideCommand command,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        var utcNow = utcClock.UtcNow;
        var activeOverride = await dbContext.FastingOverrides
            .FirstOrDefaultAsync(
                o => o.UserId == actorUserId
                    && (o.Status == FastingOverride.StatusFasting || o.Status == FastingOverride.StatusEating),
                cancellationToken);

        if (activeOverride is null)
            return Result<FastingOverrideDto>.Failure(NutritionErrors.FastingNoActiveOverride());

        activeOverride.Status = FastingOverride.StatusCancelled;
        activeOverride.CompletedAtUtc = utcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<FastingOverrideDto>.Success(FastingMapper.ToOverrideDto(activeOverride));
    }
}
