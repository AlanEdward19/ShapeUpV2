namespace IntegrationTests.Domains.Gamification;

using IntegrationTests.Infrastructure;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ShapeUp.Features.Gamification.GetGamificationProfile;
using ShapeUp.Features.Gamification.Infrastructure.Data;
using ShapeUp.Features.Gamification.NutritionGoalMet;
using ShapeUp.Features.Gamification.Shared;
using ShapeUp.Features.Gamification.Shared.Entities;
using ShapeUp.Features.Nutrition.Shared.Events;
using ShapeUp.Features.Training.Shared.Abstractions;

[Collection("SQL Server Write Operations")]
public sealed class GamificationNutritionGoalMetConsumerIntegrationTests(SqlServerFixture fixture) : IAsyncLifetime
{
    private GamificationDbContext _dbContext = null!;

    public Task InitializeAsync()
    {
        _dbContext = fixture.CreateGamificationDbContext();
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _dbContext.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Consume_WhenFirstDelivery_PersistsCreditAndEvaluation()
    {
        const int userId = 92001;
        var goalDate = new DateOnly(2026, 5, 20);
        var consumer = CreateConsumer();

        await consumer.Consume(CreateContext(new NutritionGoalMet(userId, goalDate)));

        var profile = await _dbContext.Profiles.SingleAsync(p => p.UserId == userId);
        Assert.Equal(50, profile.TotalXp);
        Assert.Equal(10, profile.ShapeCoins);
        Assert.Equal(1, profile.NutritionCurrentStreak);
        Assert.Equal(goalDate, profile.LastNutritionGoalMetDate);
        Assert.Equal(1, await _dbContext.NutritionEvaluations.CountAsync(e => e.UserId == userId && e.Date == goalDate));
    }

    [Fact]
    public async Task Consume_WhenRedelivered_DoesNotDuplicateCredit()
    {
        const int userId = 92002;
        var goalDate = new DateOnly(2026, 5, 21);
        var consumer = CreateConsumer();
        var message = new NutritionGoalMet(userId, goalDate);

        await consumer.Consume(CreateContext(message));
        await consumer.Consume(CreateContext(message));

        var profile = await _dbContext.Profiles.SingleAsync(p => p.UserId == userId);
        Assert.Equal(50, profile.TotalXp);
        Assert.Equal(10, profile.ShapeCoins);
        Assert.Equal(1, profile.NutritionCurrentStreak);
        Assert.Equal(1, await _dbContext.NutritionEvaluations.CountAsync(e => e.UserId == userId && e.Date == goalDate));
    }

    [Fact]
    public async Task GetGamificationProfile_WhenLastMetDateIsStale_ReturnsZeroDisplayedNutritionStreak()
    {
        const int userId = 92003;
        var staleDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-3));

        _dbContext.Profiles.Add(new GamificationProfile
        {
            UserId = userId,
            TotalXp = 100,
            Level = 1,
            CurrentStreak = 2,
            NutritionCurrentStreak = 5,
            LastNutritionGoalMetDate = staleDate,
            ShapeCoins = 20,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync();

        var workoutSessionRepository = new Mock<IWorkoutSessionRepository>();
        workoutSessionRepository
            .Setup(x => x.GetCompletedByUserInRangeAsync(userId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var handler = new GetGamificationProfileHandler(
            _dbContext,
            new ShapeScoreCalculator(_dbContext, workoutSessionRepository.Object));

        var result = await handler.HandleAsync(new GetGamificationProfileQuery(userId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value!.NutritionCurrentStreak);
        Assert.Equal(5, (await _dbContext.Profiles.SingleAsync(p => p.UserId == userId)).NutritionCurrentStreak);
    }

    private GamificationNutritionGoalMetConsumer CreateConsumer() =>
        new(_dbContext, Mock.Of<ILogger<GamificationNutritionGoalMetConsumer>>());

    private static ConsumeContext<NutritionGoalMet> CreateContext(NutritionGoalMet message)
    {
        var context = new Mock<ConsumeContext<NutritionGoalMet>>();
        context.Setup(x => x.Message).Returns(message);
        context.Setup(x => x.CancellationToken).Returns(CancellationToken.None);
        return context.Object;
    }
}
