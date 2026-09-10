namespace ShapeUp.Features.Nutrition.GoalEvaluation;

using Infrastructure.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Entities;
using Shared.Events;
using ShapeUp.Features.Nutrition.Shared.ValueObjects;

public sealed class NutritionGoalEvaluationJobConsumer(
    NutritionDbContext dbContext,
    IPublishEndpoint publishEndpoint,
    ILogger<NutritionGoalEvaluationJobConsumer> logger) : IJobConsumer<EvaluateNutritionGoals>
{
    public async Task Run(JobContext<EvaluateNutritionGoals> context)
    {
        var evaluationDate = context.Job.EvaluationDate
            ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));

        var days = await dbContext.DiaryDays
            .Include(d => d.Entries)
            .Where(d => d.Date == evaluationDate && d.EvaluatedAtUtc == null)
            .ToListAsync(context.CancellationToken);

        logger.LogInformation(
            "Evaluating nutrition goals for {DayCount} diary day(s) on {EvaluationDate}",
            days.Count,
            evaluationDate);

        foreach (var day in days)
            await EvaluateDayAsync(day, evaluationDate, context.CancellationToken);
    }

    private async Task EvaluateDayAsync(DiaryDay day, DateOnly evaluationDate, CancellationToken cancellationToken)
    {
        var profile = await dbContext.Profiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == day.UserId, cancellationToken);

        var totals = MacroCalculator.Sum(day.Entries.Select(e => e.ComputedMacros));
        var goalMet = profile?.ActiveGoal is MacroValueObject goal
            && NutritionGoalToleranceCalculator.AreMacrosWithinTolerance(totals, goal);

        if (goalMet)
        {
            await publishEndpoint.Publish(new NutritionGoalMet(day.UserId, evaluationDate), cancellationToken);
            logger.LogInformation(
                "Published NutritionGoalMet for user {UserId} on {EvaluationDate}",
                day.UserId,
                evaluationDate);
        }

        var evaluatedAtUtc = DateTime.UtcNow;
        day.EvaluatedAtUtc = evaluatedAtUtc;

        dbContext.GoalEvaluations.Add(new NutritionGoalEvaluation
        {
            UserId = day.UserId,
            Date = evaluationDate,
            GoalMet = goalMet,
            EvaluatedAtUtc = evaluatedAtUtc
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
