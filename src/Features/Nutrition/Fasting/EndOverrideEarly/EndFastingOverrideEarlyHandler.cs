using Microsoft.EntityFrameworkCore;
using ShapeUp.Features.Nutrition.Fasting.Shared;
using ShapeUp.Features.Nutrition.Fasting.Shared.ViewModels;
using ShapeUp.Features.Nutrition.Infrastructure.Data;
using ShapeUp.Features.Nutrition.Shared.Entities;
using ShapeUp.Features.Nutrition.Shared.Errors;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.Fasting.EndOverrideEarly;

public sealed class EndFastingOverrideEarlyHandler(
    NutritionDbContext dbContext,
    IUtcClock utcClock)
{
    public async Task<Result<FastingOverrideDto>> HandleAsync(
        EndFastingOverrideEarlyCommand command,
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

        if (activeOverride.Status == FastingOverride.StatusEating)
            return Result<FastingOverrideDto>.Failure(NutritionErrors.FastingEndEarlyWhileEating());

        activeOverride.Status = FastingOverride.StatusEating;
        activeOverride.EatEndsAtUtc = utcNow.AddHours(activeOverride.EatHours);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<FastingOverrideDto>.Success(FastingMapper.ToOverrideDto(activeOverride));
    }
}
