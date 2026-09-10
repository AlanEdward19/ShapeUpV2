namespace IntegrationTests.Domains.Nutrition;

using IntegrationTests.Infrastructure;
using Moq;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShapeUp.Features.Nutrition.GoalEvaluation;
using ShapeUp.Features.Nutrition.Infrastructure.Data;
using ShapeUp.Features.Nutrition.Shared.Entities;
using ShapeUp.Features.Nutrition.Shared.Events;
using ShapeUp.Features.Nutrition.Shared.ValueObjects;

[Collection("SQL Server Write Operations")]
public sealed class NutritionGoalEvaluationJobConsumerIntegrationTests(SqlServerFixture fixture) : IAsyncLifetime
{
    private NutritionDbContext _dbContext = null!;

    public Task InitializeAsync()
    {
        _dbContext = fixture.CreateNutritionDbContext();
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _dbContext.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Run_WhenMacrosMeetGoal_PublishesNutritionGoalMetAndMarksDayEvaluated()
    {
        const int userId = 91001;
        var evaluationDate = new DateOnly(2026, 5, 10);
        await SeedProfileAsync(userId, proteinG: 100, carbG: 200, fatG: 70);
        await SeedDiaryDayAsync(userId, evaluationDate, proteinG: 100, carbG: 200, fatG: 70);

        var published = new List<NutritionGoalMet>();
        var publishEndpoint = CreatePublishEndpoint(published);
        var consumer = CreateConsumer(publishEndpoint);

        await consumer.Run(CreateJobContext(new EvaluateNutritionGoals(evaluationDate)));

        Assert.Single(published);
        Assert.Equal(userId, published[0].UserId);
        Assert.Equal(evaluationDate, published[0].Date);

        var day = await _dbContext.DiaryDays.SingleAsync(d => d.UserId == userId && d.Date == evaluationDate);
        Assert.NotNull(day.EvaluatedAtUtc);

        var evaluation = await _dbContext.GoalEvaluations.SingleAsync(e => e.UserId == userId && e.Date == evaluationDate);
        Assert.True(evaluation.GoalMet);
    }

    [Fact]
    public async Task Run_WhenMacrosMissGoal_DoesNotPublishAndStillMarksDayEvaluated()
    {
        const int userId = 91002;
        var evaluationDate = new DateOnly(2026, 5, 11);
        await SeedProfileAsync(userId, proteinG: 100, carbG: 200, fatG: 70);
        await SeedDiaryDayAsync(userId, evaluationDate, proteinG: 80, carbG: 200, fatG: 70);

        var published = new List<NutritionGoalMet>();
        var publishEndpoint = CreatePublishEndpoint(published);
        var consumer = CreateConsumer(publishEndpoint);

        await consumer.Run(CreateJobContext(new EvaluateNutritionGoals(evaluationDate)));

        Assert.Empty(published);

        var day = await _dbContext.DiaryDays.SingleAsync(d => d.UserId == userId && d.Date == evaluationDate);
        Assert.NotNull(day.EvaluatedAtUtc);

        var evaluation = await _dbContext.GoalEvaluations.SingleAsync(e => e.UserId == userId && e.Date == evaluationDate);
        Assert.False(evaluation.GoalMet);
    }

    [Fact]
    public async Task Run_WhenDayAlreadyEvaluated_IsIdempotent()
    {
        const int userId = 91003;
        var evaluationDate = new DateOnly(2026, 5, 12);
        await SeedProfileAsync(userId, proteinG: 100, carbG: 200, fatG: 70);
        await SeedDiaryDayAsync(userId, evaluationDate, proteinG: 100, carbG: 200, fatG: 70);

        var published = new List<NutritionGoalMet>();
        var publishEndpoint = CreatePublishEndpoint(published);
        var consumer = CreateConsumer(publishEndpoint);

        await consumer.Run(CreateJobContext(new EvaluateNutritionGoals(evaluationDate)));
        Assert.Single(published);

        published.Clear();
        await consumer.Run(CreateJobContext(new EvaluateNutritionGoals(evaluationDate)));

        Assert.Empty(published);
        Assert.Equal(1, await _dbContext.GoalEvaluations.CountAsync(e => e.UserId == userId && e.Date == evaluationDate));
    }

    private NutritionGoalEvaluationJobConsumer CreateConsumer(IPublishEndpoint publishEndpoint) =>
        new(
            _dbContext,
            publishEndpoint,
            Mock.Of<ILogger<NutritionGoalEvaluationJobConsumer>>());

    private static IPublishEndpoint CreatePublishEndpoint(List<NutritionGoalMet> published)
    {
        var endpoint = new Mock<IPublishEndpoint>();
        endpoint
            .Setup(x => x.Publish(
                It.IsAny<NutritionGoalMet>(),
                It.IsAny<CancellationToken>()))
            .Callback<NutritionGoalMet, CancellationToken>((message, _) => published.Add(message))
            .Returns(Task.CompletedTask);

        return endpoint.Object;
    }

    private static JobContext<EvaluateNutritionGoals> CreateJobContext(EvaluateNutritionGoals message)
    {
        var context = new Mock<JobContext<EvaluateNutritionGoals>>();
        context.Setup(x => x.Job).Returns(message);
        context.Setup(x => x.CancellationToken).Returns(CancellationToken.None);
        return context.Object;
    }

    private async Task SeedProfileAsync(int userId, int proteinG, int carbG, int fatG)
    {
        _dbContext.Profiles.Add(new NutritionProfile
        {
            UserId = userId,
            ActiveGoal = new MacroValueObject
            {
                Kcal = 2000,
                ProteinG = proteinG,
                CarbG = carbG,
                FatG = fatG
            },
            UpdatedAtUtc = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync();
    }

    private async Task SeedDiaryDayAsync(int userId, DateOnly date, int proteinG, int carbG, int fatG)
    {
        var day = new DiaryDay
        {
            UserId = userId,
            Date = date,
            Entries =
            [
                new DiaryEntry
                {
                    Id = $"entry-{userId}-{date:yyyyMMdd}",
                    MealSlot = "lunch",
                    FoodId = "food-1",
                    QuantityGramsOrMl = 100m,
                    ComputedMacros = new MacroValueObject
                    {
                        Kcal = 0,
                        ProteinG = proteinG,
                        CarbG = carbG,
                        FatG = fatG
                    }
                }
            ]
        };

        _dbContext.DiaryDays.Add(day);
        await _dbContext.SaveChangesAsync();
    }
}
