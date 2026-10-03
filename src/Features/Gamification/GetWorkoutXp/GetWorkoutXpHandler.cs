namespace ShapeUp.Features.Gamification.GetWorkoutXp;

using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using ShapeUp.Features.Gamification.WorkoutFinished;
using ShapeUp.Shared.Results;

public class GetWorkoutXpHandler(GamificationDbContext dbContext)
{
    private const int MaxSessionIds = 100;

    public async Task<Result<IReadOnlyList<GetWorkoutXpResponse>>> HandleAsync(
        GetWorkoutXpQuery query,
        CancellationToken cancellationToken)
    {
        var ids = query.SessionIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct()
            .ToArray();

        if (ids.Length == 0)
            return Result<IReadOnlyList<GetWorkoutXpResponse>>.Success([]);

        if (ids.Length > MaxSessionIds)
            return Result<IReadOnlyList<GetWorkoutXpResponse>>.Failure(
                CommonErrors.Validation($"At most {MaxSessionIds} session ids per request."));

        var evaluations = await dbContext.Evaluations
            .AsNoTracking()
            .Where(e => e.UserId == query.UserId && ids.Contains(e.SessionId))
            .ToListAsync(cancellationToken);

        IReadOnlyList<GetWorkoutXpResponse> response = evaluations
            .Select(e => new GetWorkoutXpResponse(
                e.SessionId,
                e.CreditGranted ? GamificationWorkoutFinishedConsumer.WorkoutXpReward : 0,
                e.CreditGranted))
            .ToList();

        return Result<IReadOnlyList<GetWorkoutXpResponse>>.Success(response);
    }
}
