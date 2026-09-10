using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShapeUp.Features.Gamification.Infrastructure.Data;
using ShapeUp.Features.Gamification.NutritionGoalMet;
using ShapeUp.Features.Gamification.Shared.Entities;
using ShapeUp.Features.Nutrition.Shared.Events;

namespace UnitTests.Domains.Gamification;

public class GamificationNutritionGoalMetConsumerTests
{
    private readonly GamificationDbContext _dbContext;

    public GamificationNutritionGoalMetConsumerTests()
    {
        var options = new DbContextOptionsBuilder<GamificationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new GamificationDbContext(options);
    }

    [Fact]
    public async Task Consume_WhenFirstDelivery_CreditsXpCoinsAndIncrementsNutritionStreak()
    {
        var goalDate = new DateOnly(2026, 5, 10);
        var message = new NutritionGoalMet(42, goalDate);

        var sut = CreateSut();
        await sut.Consume(CreateContext(message));

        var profile = await _dbContext.Profiles.SingleAsync(p => p.UserId == 42);
        Assert.Equal(50, profile.TotalXp);
        Assert.Equal(10, profile.ShapeCoins);
        Assert.Equal(1, profile.NutritionCurrentStreak);
        Assert.Equal(goalDate, profile.LastNutritionGoalMetDate);

        var evaluation = await _dbContext.NutritionEvaluations.SingleAsync(e => e.UserId == 42 && e.Date == goalDate);
        Assert.True(evaluation.CreditedAtUtc <= DateTime.UtcNow);
    }

    [Fact]
    public async Task Consume_WhenSameDayRedelivered_IsNoOp()
    {
        var goalDate = new DateOnly(2026, 5, 11);
        var message = new NutritionGoalMet(42, goalDate);

        _dbContext.NutritionEvaluations.Add(new GamificationNutritionEvaluation
        {
            UserId = 42,
            Date = goalDate,
            CreditedAtUtc = DateTime.UtcNow
        });
        _dbContext.Profiles.Add(new GamificationProfile
        {
            UserId = 42,
            TotalXp = 50,
            ShapeCoins = 10,
            NutritionCurrentStreak = 1,
            LastNutritionGoalMetDate = goalDate,
            Level = 1,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync();

        var sut = CreateSut();
        await sut.Consume(CreateContext(message));

        var profile = await _dbContext.Profiles.SingleAsync(p => p.UserId == 42);
        Assert.Equal(50, profile.TotalXp);
        Assert.Equal(10, profile.ShapeCoins);
        Assert.Equal(1, profile.NutritionCurrentStreak);
        Assert.Single(await _dbContext.NutritionEvaluations.ToListAsync());
    }

    private GamificationNutritionGoalMetConsumer CreateSut() =>
        new(_dbContext, Mock.Of<ILogger<GamificationNutritionGoalMetConsumer>>());

    private static ConsumeContext<NutritionGoalMet> CreateContext(NutritionGoalMet message)
    {
        var context = new Mock<ConsumeContext<NutritionGoalMet>>();
        context.Setup(x => x.Message).Returns(message);
        context.Setup(x => x.CancellationToken).Returns(CancellationToken.None);
        return context.Object;
    }
}
