namespace ShapeUp.Features.Gamification.NutritionGoalMet;

using Infrastructure.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Entities;
using ShapeUp.Features.Nutrition.Shared.Events;

public sealed class GamificationNutritionGoalMetConsumer(
    GamificationDbContext dbContext,
    ILogger<GamificationNutritionGoalMetConsumer> logger) : IConsumer<NutritionGoalMet>
{
    private const int NutritionXpReward = 50;
    private const int NutritionShapeCoinsReward = 10;

    public async Task Consume(ConsumeContext<NutritionGoalMet> context)
    {
        var message = context.Message;
        var cancellationToken = context.CancellationToken;

        if (await dbContext.NutritionEvaluations.AnyAsync(
                e => e.UserId == message.UserId && e.Date == message.Date,
                cancellationToken))
            return;

        logger.LogInformation(
            "Nutrition goal met for user {UserId} on {Date}",
            message.UserId,
            message.Date);

        var profile = await dbContext.Profiles.FindAsync([message.UserId], cancellationToken);
        if (profile is null)
        {
            profile = new GamificationProfile
            {
                UserId = message.UserId,
                Level = LevelCalculator.CalculateFromTotalXp(0),
                UpdatedAtUtc = DateTime.UtcNow
            };
            dbContext.Profiles.Add(profile);
        }

        profile.TotalXp += NutritionXpReward;
        profile.ShapeCoins += NutritionShapeCoinsReward;
        profile.NutritionCurrentStreak = NutritionStreakCalculator.CalculateStoredStreak(
            profile.LastNutritionGoalMetDate,
            profile.NutritionCurrentStreak,
            message.Date);
        profile.LastNutritionGoalMetDate = message.Date;
        profile.Level = LevelCalculator.CalculateFromTotalXp(profile.TotalXp);
        profile.UpdatedAtUtc = DateTime.UtcNow;

        dbContext.NutritionEvaluations.Add(new GamificationNutritionEvaluation
        {
            UserId = message.UserId,
            Date = message.Date,
            CreditedAtUtc = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
