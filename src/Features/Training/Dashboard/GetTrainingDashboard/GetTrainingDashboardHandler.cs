using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Training.Dashboard.GetTrainingDashboard;

public class GetTrainingDashboardHandler(IWorkoutSessionRepository workoutSessionRepository)
{
    public async Task<Result<TrainingDashboardResponse>> HandleAsync(GetTrainingDashboardQuery query, CancellationToken cancellationToken)
    {
        if (query.SessionsTargetPerWeek <= 0)
            return Result<TrainingDashboardResponse>.Failure(CommonErrors.Validation("sessionsTargetPerWeek must be greater than zero."));

        var now = DateTime.UtcNow;
        var weekStart = StartOfWeekUtc(now.Date);
        var weekEnd = weekStart.AddDays(7);
        var previousWeekStart = weekStart.AddDays(-7);

        var thisWeek = await workoutSessionRepository.GetCompletedByUserInRangeAsync(query.TargetUserId, weekStart, weekEnd, cancellationToken);
        var previousWeek = await workoutSessionRepository.GetCompletedByUserInRangeAsync(query.TargetUserId, previousWeekStart, weekStart, cancellationToken);

        var weeklyVolume = thisWeek
            .SelectMany(s => s.Exercises)
            .SelectMany(e => e.Sets)
            .Sum(s => s.Volume);

        var previousWeekVolume = previousWeek
            .SelectMany(s => s.Exercises)
            .SelectMany(e => e.Sets)
            .Sum(s => s.Volume);

        var weeklyVolumeProgress = previousWeekVolume == 0
            ? (weeklyVolume > 0 ? 100m : 0m)
            : ((weeklyVolume - previousWeekVolume) / previousWeekVolume) * 100m;

        var sessionsCompleted = thisWeek.Count;
        var completionRate = Math.Min(100m, (decimal)sessionsCompleted / query.SessionsTargetPerWeek * 100m);
        var prCount = thisWeek.Sum(s => s.PersonalRecords.Count);

        var history = await workoutSessionRepository.GetCompletedByUserInRangeAsync(query.TargetUserId, now.AddYears(-1), now.AddDays(1), cancellationToken);
        var consecutiveDays = CalculateConsecutiveDays(history.Select(x => x.StartedAtUtc.Date).Distinct().OrderByDescending(x => x).ToArray());

        return Result<TrainingDashboardResponse>.Success(new TrainingDashboardResponse(
            weeklyVolume,
            consecutiveDays,
            sessionsCompleted,
            query.SessionsTargetPerWeek,
            completionRate,
            prCount,
            weeklyVolumeProgress));
    }

    private static DateTime StartOfWeekUtc(DateTime dateUtc)
    {
        var diff = (7 + (dateUtc.DayOfWeek - DayOfWeek.Monday)) % 7;
        return dateUtc.AddDays(-diff);
    }

    private static int CalculateConsecutiveDays(IReadOnlyList<DateTime> orderedDays)
    {
        if (orderedDays.Count == 0)
            return 0;

        var streak = 1;
        for (var index = 1; index < orderedDays.Count; index++)
        {
            if ((orderedDays[index - 1] - orderedDays[index]).TotalDays != 1)
                break;

            streak++;
        }

        return streak;
    }
}