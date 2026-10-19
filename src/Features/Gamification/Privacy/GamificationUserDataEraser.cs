namespace ShapeUp.Features.Gamification.Privacy;

using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using ShapeUp.Shared.Privacy;

public class GamificationUserDataEraser(GamificationDbContext db) : IUserDataEraser
{
    public string Module => "Gamification";

    public async Task EraseAsync(int userId, string? email, CancellationToken cancellationToken)
    {
        await db.Evaluations.Where(x => x.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        await db.NutritionEvaluations.Where(x => x.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        await db.Profiles.Where(x => x.UserId == userId).ExecuteDeleteAsync(cancellationToken);
    }
}
