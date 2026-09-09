using Microsoft.EntityFrameworkCore;
using ShapeUp.Features.Gamification.GetGamificationProfile;
using ShapeUp.Features.Gamification.Infrastructure.Data;
using ShapeUp.Features.Gamification.Shared;
using ShapeUp.Features.Gamification.Shared.Entities;
using ShapeUp.Features.Gamification.Shared.Enums;
using ShapeUp.Features.Training.Shared.Abstractions;

namespace UnitTests.Domains.Gamification;

public class GetGamificationProfileHandlerTests
{
    private const int UserId = 42;
    private readonly GamificationDbContext _dbContext;
    private readonly Mock<IWorkoutSessionRepository> _workoutSessionRepository = new();

    public GetGamificationProfileHandlerTests()
    {
        var options = new DbContextOptionsBuilder<GamificationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new GamificationDbContext(options);
    }

    [Fact]
    public async Task HandleAsync_WhenProfileMissing_ReturnsZeroedSuccessResponse()
    {
        _workoutSessionRepository
            .Setup(x => x.GetCompletedByUserInRangeAsync(UserId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var handler = CreateHandler();
        var result = await handler.HandleAsync(new GetGamificationProfileQuery(UserId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(0, result.Value!.TotalXp);
        Assert.Equal(1, result.Value.Level);
        Assert.Equal(0, result.Value.CurrentStreak);
        Assert.Equal(0, result.Value.ShapeCoins);
        Assert.Equal(0, result.Value.ShapeScore);
        Assert.False(result.Value.LastEvaluationLeveledUp);
        Assert.Null(result.Value.LastEvaluationLevelFrom);
        Assert.Null(result.Value.LastEvaluationLevelTo);
        Assert.False(result.Value.LastEvaluationStreakMilestoneHit);
        Assert.Null(result.Value.LastEvaluationStreakMilestoneValue);
    }

    [Fact]
    public async Task HandleAsync_WhenProfileExists_ReturnsStoredValuesAndCalculatedShapeScore()
    {
        var now = DateTime.UtcNow;
        await _dbContext.Profiles.AddAsync(new GamificationProfile
        {
            UserId = UserId,
            TotalXp = 750,
            Level = 2,
            CurrentStreak = 5,
            ShapeCoins = 120,
            LastEvaluationLeveledUp = true,
            LastEvaluationLevelFrom = 1,
            LastEvaluationLevelTo = 2,
            LastEvaluationStreakMilestoneHit = true,
            LastEvaluationStreakMilestoneValue = 7,
            UpdatedAtUtc = now
        });

        await _dbContext.Evaluations.AddAsync(new WorkoutEvaluation
        {
            SessionId = "session-1",
            UserId = UserId,
            Classification = ActivityClassification.Verified,
            CreditGranted = true,
            EvaluatedAtUtc = now.AddDays(-1)
        });

        await _dbContext.SaveChangesAsync();

        _workoutSessionRepository
            .Setup(x => x.GetCompletedByUserInRangeAsync(UserId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var handler = CreateHandler();
        var result = await handler.HandleAsync(new GetGamificationProfileQuery(UserId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(750, result.Value!.TotalXp);
        Assert.Equal(2, result.Value.Level);
        Assert.Equal(5, result.Value.CurrentStreak);
        Assert.Equal(120, result.Value.ShapeCoins);
        Assert.Equal(1, result.Value.ShapeScore);
        Assert.True(result.Value.LastEvaluationLeveledUp);
        Assert.Equal(1, result.Value.LastEvaluationLevelFrom);
        Assert.Equal(2, result.Value.LastEvaluationLevelTo);
        Assert.True(result.Value.LastEvaluationStreakMilestoneHit);
        Assert.Equal(7, result.Value.LastEvaluationStreakMilestoneValue);
    }

    private GetGamificationProfileHandler CreateHandler() =>
        new(_dbContext, new ShapeScoreCalculator(_dbContext, _workoutSessionRepository.Object));
}
