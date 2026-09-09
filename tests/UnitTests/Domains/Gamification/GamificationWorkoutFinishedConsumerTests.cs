using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShapeUp.Features.Gamification.Infrastructure.Data;
using ShapeUp.Features.Gamification.Shared.AntiCheat;
using ShapeUp.Features.Gamification.Shared.Entities;
using ShapeUp.Features.Gamification.Shared.Enums;
using ShapeUp.Features.Gamification.WorkoutFinished;
using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Documents;
using ShapeUp.Features.Training.Shared.Events;

namespace UnitTests.Domains.Gamification;

public class GamificationWorkoutFinishedConsumerTests
{
    private readonly GamificationDbContext _dbContext;
    private readonly Mock<IWorkoutSessionRepository> _workoutSessionRepository = new();
    private readonly Mock<IAntiCheatClassifier> _antiCheatClassifier = new();
    private readonly Mock<ILogger<GamificationWorkoutFinishedConsumer>> _logger = new();

    public GamificationWorkoutFinishedConsumerTests()
    {
        var options = new DbContextOptionsBuilder<GamificationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new GamificationDbContext(options);
    }

    [Theory]
    [InlineData(ActivityClassification.Verified)]
    [InlineData(ActivityClassification.LikelyValid)]
    public async Task Consume_WhenClassificationGrantsCredit_UpdatesProfileAndPersistsEvaluation(
        ActivityClassification classification)
    {
        var endedAtUtc = new DateTime(2026, 3, 10, 12, 0, 0, DateTimeKind.Utc);
        var message = new WorkoutFinished("session-1", 10, 42, endedAtUtc);
        var session = CreateSession("session-1", 42, endedAtUtc);

        SetupSessionLookup(session);
        _antiCheatClassifier
            .Setup(x => x.ClassifyAsync(session, It.IsAny<IReadOnlyList<WorkoutSessionDocument>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AntiCheatResult(classification, null));

        var sut = CreateSut();
        await sut.Consume(CreateContext(message));

        var profile = await _dbContext.Profiles.SingleAsync(p => p.UserId == 42);
        Assert.Equal(50, profile.TotalXp);
        Assert.Equal(10, profile.ShapeCoins);
        Assert.Equal(1, profile.CurrentStreak);
        Assert.Equal(1, profile.Level);
        Assert.Equal(endedAtUtc.Date, profile.LastActivityDateUtc);

        var evaluation = await _dbContext.Evaluations.SingleAsync(e => e.SessionId == "session-1");
        Assert.Equal(classification, evaluation.Classification);
        Assert.True(evaluation.CreditGranted);
        Assert.Equal(42, evaluation.UserId);
    }

    [Theory]
    [InlineData(ActivityClassification.Suspicious)]
    [InlineData(ActivityClassification.Invalid)]
    public async Task Consume_WhenClassificationWithholdsCredit_DoesNotUpdateProfileButPersistsEvaluation(
        ActivityClassification classification)
    {
        var endedAtUtc = new DateTime(2026, 3, 10, 12, 0, 0, DateTimeKind.Utc);
        var message = new WorkoutFinished("session-2", 10, 42, endedAtUtc);
        var session = CreateSession("session-2", 42, endedAtUtc);

        _dbContext.Profiles.Add(new GamificationProfile
        {
            UserId = 42,
            TotalXp = 100,
            Level = 1,
            CurrentStreak = 3,
            ShapeCoins = 25,
            LastActivityDateUtc = endedAtUtc.AddDays(-1).Date,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync();

        SetupSessionLookup(session);
        _antiCheatClassifier
            .Setup(x => x.ClassifyAsync(session, It.IsAny<IReadOnlyList<WorkoutSessionDocument>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AntiCheatResult(classification, "test reason"));

        var sut = CreateSut();
        await sut.Consume(CreateContext(message));

        var profile = await _dbContext.Profiles.SingleAsync(p => p.UserId == 42);
        Assert.Equal(100, profile.TotalXp);
        Assert.Equal(25, profile.ShapeCoins);
        Assert.Equal(3, profile.CurrentStreak);
        Assert.False(profile.LastEvaluationLeveledUp);
        Assert.Null(profile.LastEvaluationLevelFrom);
        Assert.Null(profile.LastEvaluationLevelTo);
        Assert.False(profile.LastEvaluationStreakMilestoneHit);
        Assert.Null(profile.LastEvaluationStreakMilestoneValue);

        var evaluation = await _dbContext.Evaluations.SingleAsync(e => e.SessionId == "session-2");
        Assert.Equal(classification, evaluation.Classification);
        Assert.False(evaluation.CreditGranted);
        Assert.Equal("test reason", evaluation.Reason);
    }

    [Fact]
    public async Task Consume_WhenSessionAlreadyEvaluated_IsNoOp()
    {
        var endedAtUtc = new DateTime(2026, 3, 10, 12, 0, 0, DateTimeKind.Utc);
        var message = new WorkoutFinished("session-3", 10, 42, endedAtUtc);

        _dbContext.Evaluations.Add(new WorkoutEvaluation
        {
            SessionId = "session-3",
            UserId = 42,
            Classification = ActivityClassification.Verified,
            CreditGranted = true,
            EvaluatedAtUtc = DateTime.UtcNow
        });
        _dbContext.Profiles.Add(new GamificationProfile
        {
            UserId = 42,
            TotalXp = 50,
            Level = 1,
            ShapeCoins = 10,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync();

        var sut = CreateSut();
        await sut.Consume(CreateContext(message));

        _workoutSessionRepository.Verify(
            x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);

        var profile = await _dbContext.Profiles.SingleAsync(p => p.UserId == 42);
        Assert.Equal(50, profile.TotalXp);
        Assert.Equal(10, profile.ShapeCoins);
        Assert.Single(await _dbContext.Evaluations.ToListAsync());
    }

    [Fact]
    public async Task Consume_WhenStreakReachesSeven_AwardsMilestoneBonusOnce()
    {
        var endedAtUtc = new DateTime(2026, 3, 17, 12, 0, 0, DateTimeKind.Utc);
        var message = new WorkoutFinished("session-7", 10, 42, endedAtUtc);
        var session = CreateSession("session-7", 42, endedAtUtc);

        _dbContext.Profiles.Add(new GamificationProfile
        {
            UserId = 42,
            TotalXp = 0,
            Level = 1,
            CurrentStreak = 6,
            LastActivityDateUtc = endedAtUtc.AddDays(-1).Date,
            ShapeCoins = 0,
            LastStreakMilestoneAwarded = 0,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync();

        SetupSessionLookup(session);
        _antiCheatClassifier
            .Setup(x => x.ClassifyAsync(session, It.IsAny<IReadOnlyList<WorkoutSessionDocument>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AntiCheatResult(ActivityClassification.Verified, null));

        var sut = CreateSut();
        await sut.Consume(CreateContext(message));

        var profile = await _dbContext.Profiles.SingleAsync(p => p.UserId == 42);
        Assert.Equal(7, profile.CurrentStreak);
        Assert.Equal(60, profile.ShapeCoins);
        Assert.Equal(7, profile.LastStreakMilestoneAwarded);
        Assert.True(profile.LastEvaluationStreakMilestoneHit);
        Assert.Equal(7, profile.LastEvaluationStreakMilestoneValue);
    }

    [Fact]
    public async Task Consume_WhenStreakAlreadyPassedMilestone_DoesNotAwardAgain()
    {
        var endedAtUtc = new DateTime(2026, 3, 18, 12, 0, 0, DateTimeKind.Utc);
        var message = new WorkoutFinished("session-8", 10, 42, endedAtUtc);
        var session = CreateSession("session-8", 42, endedAtUtc);

        _dbContext.Profiles.Add(new GamificationProfile
        {
            UserId = 42,
            TotalXp = 50,
            Level = 1,
            CurrentStreak = 7,
            LastActivityDateUtc = endedAtUtc.AddDays(-1).Date,
            ShapeCoins = 60,
            LastStreakMilestoneAwarded = 7,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync();

        SetupSessionLookup(session);
        _antiCheatClassifier
            .Setup(x => x.ClassifyAsync(session, It.IsAny<IReadOnlyList<WorkoutSessionDocument>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AntiCheatResult(ActivityClassification.Verified, null));

        var sut = CreateSut();
        await sut.Consume(CreateContext(message));

        var profile = await _dbContext.Profiles.SingleAsync(p => p.UserId == 42);
        Assert.Equal(8, profile.CurrentStreak);
        Assert.Equal(70, profile.ShapeCoins);
        Assert.Equal(7, profile.LastStreakMilestoneAwarded);
        Assert.False(profile.LastEvaluationStreakMilestoneHit);
        Assert.Null(profile.LastEvaluationStreakMilestoneValue);
    }

    [Fact]
    public async Task Consume_WhenXpCrossesLevelThreshold_RecordsLevelUpSnapshot()
    {
        var endedAtUtc = new DateTime(2026, 3, 10, 12, 0, 0, DateTimeKind.Utc);
        var message = new WorkoutFinished("session-level", 10, 42, endedAtUtc);
        var session = CreateSession("session-level", 42, endedAtUtc);

        _dbContext.Profiles.Add(new GamificationProfile
        {
            UserId = 42,
            TotalXp = 480,
            Level = 1,
            CurrentStreak = 0,
            ShapeCoins = 0,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync();

        SetupSessionLookup(session);
        _antiCheatClassifier
            .Setup(x => x.ClassifyAsync(session, It.IsAny<IReadOnlyList<WorkoutSessionDocument>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AntiCheatResult(ActivityClassification.Verified, null));

        var sut = CreateSut();
        await sut.Consume(CreateContext(message));

        var profile = await _dbContext.Profiles.SingleAsync(p => p.UserId == 42);
        Assert.Equal(530, profile.TotalXp);
        Assert.Equal(2, profile.Level);
        Assert.True(profile.LastEvaluationLeveledUp);
        Assert.Equal(1, profile.LastEvaluationLevelFrom);
        Assert.Equal(2, profile.LastEvaluationLevelTo);
        Assert.False(profile.LastEvaluationStreakMilestoneHit);
    }

    private GamificationWorkoutFinishedConsumer CreateSut() =>
        new(_dbContext, _workoutSessionRepository.Object, _antiCheatClassifier.Object, _logger.Object);

    private void SetupSessionLookup(WorkoutSessionDocument session)
    {
        _workoutSessionRepository
            .Setup(x => x.GetByIdAsync(session.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        _workoutSessionRepository
            .Setup(x => x.GetCompletedByUserInRangeAsync(
                session.ExecutedByUserId,
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
    }

    private static ConsumeContext<WorkoutFinished> CreateContext(WorkoutFinished message)
    {
        var context = new Mock<ConsumeContext<WorkoutFinished>>();
        context.Setup(x => x.Message).Returns(message);
        context.Setup(x => x.CancellationToken).Returns(CancellationToken.None);
        return context.Object;
    }

    private static WorkoutSessionDocument CreateSession(string id, int executedByUserId, DateTime endedAtUtc) =>
        AntiCheatClassifierTestHelpers.CreateDurationRatioSession(id, executedByUserId, endedAtUtc, ratio: 0.9);
}
