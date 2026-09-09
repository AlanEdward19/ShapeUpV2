namespace ShapeUp.Features.Gamification.WorkoutFinished;

using Infrastructure.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.AntiCheat;
using Shared.Entities;
using Shared.Enums;
using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Events;

public sealed class GamificationWorkoutFinishedConsumer(
    GamificationDbContext dbContext,
    IWorkoutSessionRepository workoutSessionRepository,
    IAntiCheatClassifier antiCheatClassifier,
    ILogger<GamificationWorkoutFinishedConsumer> logger) : IConsumer<WorkoutFinished>
{
    private const int WorkoutXpReward = 50;
    private const int WorkoutShapeCoinsReward = 10;
    private const int StreakMilestoneCoinsBonus = 50;
    private static readonly TimeSpan DuplicationLookback = TimeSpan.FromMinutes(5);

    public async Task Consume(ConsumeContext<WorkoutFinished> context)
    {
        var message = context.Message;
        var cancellationToken = context.CancellationToken;

        if (await dbContext.Evaluations.AnyAsync(e => e.SessionId == message.SessionId, cancellationToken))
            return;

        logger.LogInformation(
            "Workout finished for session {SessionId} (target user {TargetUserId}, executed by {ExecutedByUserId}, ended at {EndedAtUtc})",
            message.SessionId,
            message.TargetUserId,
            message.ExecutedByUserId,
            message.EndedAtUtc);

        var session = await workoutSessionRepository.GetByIdAsync(message.SessionId, cancellationToken);
        if (session is null)
        {
            // Session is expected to exist (outbox publishes after the same transaction). We log and
            // complete without persisting so a rare race does not poison the queue with endless retries.
            logger.LogWarning(
                "Skipping gamification evaluation for session {SessionId}: workout session not found",
                message.SessionId);
            return;
        }

        var activityEndUtc = session.EndedAtUtc ?? message.EndedAtUtc;
        var rangeStartUtc = activityEndUtc.Add(-DuplicationLookback).AddDays(-90);
        var rangeEndUtc = activityEndUtc.AddMinutes(1);

        var recentSessions = await workoutSessionRepository.GetCompletedByUserInRangeAsync(
            message.ExecutedByUserId,
            rangeStartUtc,
            rangeEndUtc,
            cancellationToken);

        var classificationResult = await antiCheatClassifier.ClassifyAsync(session, recentSessions, cancellationToken);
        var creditGranted = classificationResult.Classification is ActivityClassification.Verified
            or ActivityClassification.LikelyValid;

        var profile = await dbContext.Profiles.FindAsync([message.ExecutedByUserId], cancellationToken);
        if (profile is null)
        {
            profile = new GamificationProfile
            {
                UserId = message.ExecutedByUserId,
                Level = LevelCalculator.CalculateFromTotalXp(0),
                UpdatedAtUtc = DateTime.UtcNow
            };
            dbContext.Profiles.Add(profile);
        }

        ResetLastEvaluationSnapshot(profile);

        if (creditGranted)
            ApplyCredit(profile, activityEndUtc);

        dbContext.Evaluations.Add(new WorkoutEvaluation
        {
            SessionId = message.SessionId,
            UserId = message.ExecutedByUserId,
            Classification = classificationResult.Classification,
            Reason = classificationResult.Reason,
            CreditGranted = creditGranted,
            EvaluatedAtUtc = DateTime.UtcNow
        });

        profile.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static void ResetLastEvaluationSnapshot(GamificationProfile profile)
    {
        profile.LastEvaluationLeveledUp = false;
        profile.LastEvaluationLevelFrom = null;
        profile.LastEvaluationLevelTo = null;
        profile.LastEvaluationStreakMilestoneHit = false;
        profile.LastEvaluationStreakMilestoneValue = null;
    }

    private static void ApplyCredit(GamificationProfile profile, DateTime activityDateUtc)
    {
        var previousLevel = profile.Level;

        profile.TotalXp += WorkoutXpReward;
        profile.ShapeCoins += WorkoutShapeCoinsReward;

        var newStreak = StreakCalculator.Calculate(profile.LastActivityDateUtc, profile.CurrentStreak, activityDateUtc);
        profile.CurrentStreak = newStreak;
        profile.LastActivityDateUtc = activityDateUtc.Date;

        var newLevel = LevelCalculator.CalculateFromTotalXp(profile.TotalXp);
        profile.Level = newLevel;

        if (newLevel > previousLevel)
        {
            profile.LastEvaluationLeveledUp = true;
            profile.LastEvaluationLevelFrom = previousLevel;
            profile.LastEvaluationLevelTo = newLevel;
        }

        if (newStreak > 0 && newStreak % 7 == 0 && profile.LastStreakMilestoneAwarded < newStreak)
        {
            profile.ShapeCoins += StreakMilestoneCoinsBonus;
            profile.LastStreakMilestoneAwarded = newStreak;
            profile.LastEvaluationStreakMilestoneHit = true;
            profile.LastEvaluationStreakMilestoneValue = newStreak;
        }
    }
}
