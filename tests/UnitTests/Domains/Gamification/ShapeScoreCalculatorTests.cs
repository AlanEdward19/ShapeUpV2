using Microsoft.EntityFrameworkCore;
using Moq;
using ShapeUp.Features.Gamification.Infrastructure.Data;
using ShapeUp.Features.Gamification.Shared;
using ShapeUp.Features.Gamification.Shared.Entities;
using ShapeUp.Features.Gamification.Shared.Enums;
using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Documents;
using ShapeUp.Features.Training.Shared.Documents.ValueObjects;

namespace UnitTests.Domains.Gamification;

public class ShapeScoreCalculatorTests
{
    private const int UserId = 42;
    private readonly GamificationDbContext _dbContext;
    private readonly Mock<IWorkoutSessionRepository> _workoutSessionRepository = new();

    public ShapeScoreCalculatorTests()
    {
        var options = new DbContextOptionsBuilder<GamificationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new GamificationDbContext(options);
    }

    [Fact]
    public async Task CalculateAsync_WhenNoActivityInWindow_ReturnsZero()
    {
        SetupSessions([]);

        var score = await CreateSut().CalculateAsync(UserId, CancellationToken.None);

        Assert.Equal(0, score);
    }

    [Fact]
    public async Task CalculateAsync_Consistencia_UsesVerifiedDaysOverThirty()
    {
        var now = DateTime.UtcNow;
        for (var dayOffset = 0; dayOffset < 15; dayOffset++)
        {
            await _dbContext.Evaluations.AddAsync(new WorkoutEvaluation
            {
                SessionId = $"consistency-{dayOffset}",
                UserId = UserId,
                Classification = ActivityClassification.Verified,
                CreditGranted = true,
                EvaluatedAtUtc = now.AddDays(-dayOffset)
            });
        }

        await _dbContext.SaveChangesAsync();
        SetupSessions([]);

        var score = await CreateSut().CalculateAsync(UserId, CancellationToken.None);

        var expected = ExpectedScore(consistencia: 15m / 30m);
        Assert.Equal(expected, score);
    }

    [Fact]
    public async Task CalculateAsync_Evolucao_ReturnsOneWhenAnySessionHasPersonalRecords()
    {
        var now = DateTime.UtcNow;
        var session = CreateSession("evolution-1", now.AddDays(-2), personalRecords:
        [
            new WorkoutPrDocumentValueObject
            {
                ExerciseId = 1,
                ExerciseName = "Bench Press",
                Type = "max_load",
                Value = 100m
            }
        ]);

        SetupSessions([session]);

        var score = await CreateSut().CalculateAsync(UserId, CancellationToken.None);

        var expected = ExpectedScore(evolucao: 1m);
        Assert.Equal(expected, score);
    }

    [Fact]
    public async Task CalculateAsync_Metas_UsesFractionOfWeeksMeetingDefaultTarget()
    {
        var now = DateTime.UtcNow;
        var weekStart = StartOfWeekUtc(now.Date);
        var sessions = Enumerable.Range(0, ShapeScoreCalculator.DefaultSessionsTargetPerWeek)
            .Select(index => CreateSession($"metas-{index}", weekStart.AddDays(index)))
            .ToList();

        SetupSessions(sessions);

        var score = await CreateSut().CalculateAsync(UserId, CancellationToken.None);

        var windowEndUtc = DateTime.UtcNow;
        var windowStartUtc = windowEndUtc.AddDays(-30);
        var weekBucketCount = CountWeekBuckets(windowStartUtc, windowEndUtc);
        var expected = ExpectedScore(metas: 1m / weekBucketCount);

        Assert.Equal(expected, score);
    }

    [Fact]
    public async Task CalculateAsync_Verificacao_UsesFractionOfVerifiedSessions()
    {
        var now = DateTime.UtcNow;
        var verifiedSession = CreateSession("verified-1", now.AddDays(-1));
        var suspiciousSession = CreateSession("suspicious-1", now.AddDays(-2));

        await _dbContext.Evaluations.AddRangeAsync(
            new WorkoutEvaluation
            {
                SessionId = verifiedSession.Id,
                UserId = UserId,
                Classification = ActivityClassification.Verified,
                CreditGranted = true,
                EvaluatedAtUtc = now.AddDays(-1)
            },
            new WorkoutEvaluation
            {
                SessionId = suspiciousSession.Id,
                UserId = UserId,
                Classification = ActivityClassification.Suspicious,
                CreditGranted = false,
                EvaluatedAtUtc = now.AddDays(-2)
            });
        await _dbContext.SaveChangesAsync();

        SetupSessions([verifiedSession, suspiciousSession]);

        var score = await CreateSut().CalculateAsync(UserId, CancellationToken.None);

        var expected = ExpectedScore(
            consistencia: 1m / 30m,
            verificacao: 0.5m);
        Assert.Equal(expected, score);
    }

    [Fact]
    public async Task CalculateAsync_AveragesAllFourSubScores()
    {
        var now = DateTime.UtcNow;
        var weekStart = StartOfWeekUtc(now.Date);

        var sessions = Enumerable.Range(0, ShapeScoreCalculator.DefaultSessionsTargetPerWeek)
            .Select(index => CreateSession(
                $"combined-{index}",
                weekStart.AddDays(index),
                personalRecords: index == 0
                    ?
                    [
                        new WorkoutPrDocumentValueObject
                        {
                            ExerciseId = 1,
                            ExerciseName = "Squat",
                            Type = "max_load",
                            Value = 120m
                        }
                    ]
                    : []))
            .ToList();

        await _dbContext.Evaluations.AddRangeAsync(
            sessions.Select(session => new WorkoutEvaluation
            {
                SessionId = session.Id,
                UserId = UserId,
                Classification = ActivityClassification.Verified,
                CreditGranted = true,
                EvaluatedAtUtc = now.AddDays(-1)
            }));
        await _dbContext.SaveChangesAsync();

        SetupSessions(sessions);

        var score = await CreateSut().CalculateAsync(UserId, CancellationToken.None);

        var windowEndUtc = DateTime.UtcNow;
        var windowStartUtc = windowEndUtc.AddDays(-30);
        var weekBucketCount = CountWeekBuckets(windowStartUtc, windowEndUtc);
        var expected = ExpectedScore(
            consistencia: 1m / 30m,
            evolucao: 1m,
            metas: 1m / weekBucketCount,
            verificacao: 1m);

        Assert.Equal(expected, score);
    }

    private ShapeScoreCalculator CreateSut() =>
        new(_dbContext, _workoutSessionRepository.Object);

    private void SetupSessions(IReadOnlyList<WorkoutSessionDocument> sessions)
    {
        _workoutSessionRepository
            .Setup(x => x.GetCompletedByUserInRangeAsync(
                UserId,
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessions);
    }

    private static WorkoutSessionDocument CreateSession(
        string id,
        DateTime startedAtUtc,
        List<WorkoutPrDocumentValueObject>? personalRecords = null) =>
        new()
        {
            Id = id,
            TargetUserId = UserId,
            ExecutedByUserId = UserId,
            StartedAtUtc = startedAtUtc,
            EndedAtUtc = startedAtUtc.AddHours(1),
            IsCompleted = true,
            PersonalRecords = personalRecords ?? []
        };

    private static int ExpectedScore(
        decimal consistencia = 0m,
        decimal evolucao = 0m,
        decimal metas = 0m,
        decimal verificacao = 0m)
    {
        var average = (consistencia + evolucao + metas + verificacao) / 4m;
        return (int)Math.Round(average * 100m, MidpointRounding.AwayFromZero);
    }

    private static int CountWeekBuckets(DateTime windowStartUtc, DateTime windowEndUtc)
    {
        var count = 0;

        for (var weekStart = StartOfWeekUtc(windowStartUtc.Date);
             weekStart < windowEndUtc;
             weekStart = weekStart.AddDays(7))
        {
            var weekEnd = weekStart.AddDays(7);
            var bucketStart = weekStart < windowStartUtc ? windowStartUtc : weekStart;
            var bucketEnd = weekEnd > windowEndUtc ? windowEndUtc : weekEnd;

            if (bucketStart < bucketEnd)
                count++;
        }

        return count;
    }

    private static DateTime StartOfWeekUtc(DateTime dateUtc)
    {
        var diff = (7 + (dateUtc.DayOfWeek - DayOfWeek.Monday)) % 7;
        return dateUtc.AddDays(-diff);
    }
}
