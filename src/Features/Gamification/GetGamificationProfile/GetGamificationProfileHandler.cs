namespace ShapeUp.Features.Gamification.GetGamificationProfile;

using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Entities;
using ShapeUp.Shared.Results;

public class GetGamificationProfileHandler(
    GamificationDbContext dbContext,
    ShapeScoreCalculator shapeScoreCalculator)
{
    public async Task<Result<GamificationProfileResponse>> HandleAsync(
        GetGamificationProfileQuery query,
        CancellationToken cancellationToken)
    {
        var profile = await dbContext.Profiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == query.UserId, cancellationToken);

        if (profile is null)
            return Result<GamificationProfileResponse>.Success(CreateZeroedResponse());

        var shapeScore = await shapeScoreCalculator.CalculateAsync(query.UserId, cancellationToken);

        return Result<GamificationProfileResponse>.Success(MapToResponse(profile, shapeScore));
    }

    private static GamificationProfileResponse CreateZeroedResponse() =>
        new(
            TotalXp: 0,
            Level: LevelCalculator.CalculateFromTotalXp(0),
            CurrentStreak: 0,
            ShapeCoins: 0,
            ShapeScore: 0,
            LastEvaluationLeveledUp: false,
            LastEvaluationLevelFrom: null,
            LastEvaluationLevelTo: null,
            LastEvaluationStreakMilestoneHit: false,
            LastEvaluationStreakMilestoneValue: null);

    private static GamificationProfileResponse MapToResponse(GamificationProfile profile, int shapeScore) =>
        new(
            TotalXp: profile.TotalXp,
            Level: profile.Level,
            CurrentStreak: profile.CurrentStreak,
            ShapeCoins: profile.ShapeCoins,
            ShapeScore: shapeScore,
            LastEvaluationLeveledUp: profile.LastEvaluationLeveledUp,
            LastEvaluationLevelFrom: profile.LastEvaluationLevelFrom,
            LastEvaluationLevelTo: profile.LastEvaluationLevelTo,
            LastEvaluationStreakMilestoneHit: profile.LastEvaluationStreakMilestoneHit,
            LastEvaluationStreakMilestoneValue: profile.LastEvaluationStreakMilestoneValue);
}
