using ShapeUp.Features.Entitlements.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Documents;
using ShapeUp.Features.Training.Shared.Documents.ValueObjects;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Training.Dashboard.GetWeeklyReading;

public class GetWeeklyReadingHandler(
    IWorkoutSessionRepository workoutSessionRepository,
    IEntitlementRepository entitlementRepository)
{
    public const string RequiredCapability = "weeklyProgress";

    public async Task<Result<WeeklyReadingResponse>> HandleAsync(GetWeeklyReadingQuery query, CancellationToken cancellationToken)
    {
        // The reading belongs to the Progresso plan: without it nothing of the summary is returned.
        var entitlement = await entitlementRepository.GetEntitlementAsync(query.UserId, cancellationToken);
        if (!entitlement.GrantedCapabilities.Contains(RequiredCapability))
            return Result<WeeklyReadingResponse>.Failure(CommonErrors.Forbidden("The weekly progress reading is not unlocked for this plan."));

        var weekStart = StartOfWeekUtc(DateTime.UtcNow.Date);
        var weekEnd = weekStart.AddDays(7);
        var previousWeekStart = weekStart.AddDays(-7);

        var thisWeek = (await workoutSessionRepository.GetCompletedByUserInRangeAsync(query.UserId, weekStart, weekEnd, cancellationToken))
            .Where(HasPerformedSet)
            .ToList();
        var previousWeek = (await workoutSessionRepository.GetCompletedByUserInRangeAsync(query.UserId, previousWeekStart, weekStart, cancellationToken))
            .Where(HasPerformedSet)
            .ToList();

        var daysWithWork = thisWeek
            .Select(s => s.StartedAtUtc.Date)
            .Distinct()
            .Count();

        var previousMaxLoads = MaxLoadByExercise(previousWeek);

        var trends = MaxLoadByExercise(thisWeek)
            .Select(current =>
            {
                decimal? previous = previousMaxLoads.TryGetValue(current.Key, out var previousLoad) ? previousLoad.MaxLoad : null;
                return new ExerciseLoadTrendResponse(
                    current.Key,
                    current.Value.ExerciseName,
                    current.Value.MaxLoad,
                    previous,
                    previous is null ? LoadTrend.NoPrevious
                        : current.Value.MaxLoad > previous ? LoadTrend.Up
                        : current.Value.MaxLoad < previous ? LoadTrend.Down
                        : LoadTrend.Same);
            })
            .OrderBy(t => t.ExerciseName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Result<WeeklyReadingResponse>.Success(new WeeklyReadingResponse(weekStart, daysWithWork, trends));
    }

    // Sets prefilled from the plan (IsPerformed == false) are not work. Documents stored before the flag
    // existed (null) keep counting when they carry repetitions or duration.
    private static bool IsPerformedSet(ExecutedSetDocumentValueObject set) =>
        set.IsPerformed != false && (set.Repetitions is > 0 || set.DurationSeconds is > 0);

    private static bool HasPerformedSet(WorkoutSessionDocument session) =>
        session.Exercises.Any(e => e.Sets.Any(IsPerformedSet));

    private static bool IsLoadedSet(ExecutedSetDocumentValueObject set) =>
        IsPerformedSet(set) && set.Load is > 0 && set.Repetitions is > 0;

    private static Dictionary<int, (string ExerciseName, decimal MaxLoad)> MaxLoadByExercise(IEnumerable<WorkoutSessionDocument> sessions) =>
        sessions
            .SelectMany(s => s.Exercises)
            .Where(e => e.Sets.Any(IsLoadedSet))
            .GroupBy(e => e.ExerciseId)
            .ToDictionary(
                g => g.Key,
                g => (
                    g.First().ExerciseName,
                    g.SelectMany(e => e.Sets)
                        .Where(IsLoadedSet)
                        .Max(s => s.Load!.Value)));

    private static DateTime StartOfWeekUtc(DateTime dateUtc)
    {
        var diff = (7 + (dateUtc.DayOfWeek - DayOfWeek.Monday)) % 7;
        return dateUtc.AddDays(-diff);
    }
}
