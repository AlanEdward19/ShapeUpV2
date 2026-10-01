using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Documents;
using ShapeUp.Features.Training.Shared.Documents.ValueObjects;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Training.Workouts.GetTodayWorkoutSession;

public class GetTodayWorkoutSessionHandler(IWorkoutSessionRepository workoutSessionRepository)
{
    private static readonly TimeSpan HistoryWindow = TimeSpan.FromDays(365);

    public async Task<Result<GetTodayWorkoutSessionResponse>> HandleAsync(int actorUserId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        // Today's session is the open one that already started: a session prepared for a later day is not today's.
        var session = await workoutSessionRepository.GetActiveByTargetUserIdAsync(actorUserId, cancellationToken);
        if (session is null || session.StartedAtUtc >= now.Date.AddDays(1))
            return Result<GetTodayWorkoutSessionResponse>.Success(new GetTodayWorkoutSessionResponse(false, null));

        // Newest first.
        var history = await workoutSessionRepository.GetCompletedByUserInRangeAsync(
            actorUserId, now - HistoryWindow, now.AddDays(1), cancellationToken);

        var exercises = session.Exercises
            .Select(exercise =>
            {
                var lastSets = LastPerformedSets(history, exercise.ExerciseId);
                return new TodayExerciseDto(
                    exercise.ExerciseId,
                    exercise.ExerciseName,
                    exercise.ExerciseType,
                    exercise.Sets.Select((set, index) => ToDto(set, lastSets, index)).ToArray());
            })
            .ToArray();

        return Result<GetTodayWorkoutSessionResponse>.Success(new GetTodayWorkoutSessionResponse(
            true,
            new TodayWorkoutSessionDto(session.Id, session.WorkoutPlanId, session.StartedAtUtc, exercises)));
    }

    private static IReadOnlyList<ExecutedSetDocumentValueObject> LastPerformedSets(IReadOnlyList<WorkoutSessionDocument> history, int exerciseId)
    {
        foreach (var past in history)
        {
            var sets = past.Exercises
                .Where(e => e.ExerciseId == exerciseId)
                .SelectMany(e => e.Sets)
                .Where(IsPerformed)
                .ToList();
            if (sets.Count > 0)
                return sets;
        }

        return [];
    }

    // Sets prefilled from the plan (IsPerformed == false) are not a registered record; documents from before the flag (null) count.
    private static bool IsPerformed(ExecutedSetDocumentValueObject set) =>
        set.IsPerformed != false && (set.Repetitions is > 0 || set.DurationSeconds is > 0);

    private static TodaySetDto ToDto(ExecutedSetDocumentValueObject set, IReadOnlyList<ExecutedSetDocumentValueObject> lastSets, int index)
    {
        // Set N repeats set N of the last record; extra sets repeat its last set.
        var last = lastSets.Count == 0 ? null : lastSets[Math.Min(index, lastSets.Count - 1)];

        return new TodaySetDto(
            set.Repetitions,
            set.Load,
            set.LoadUnit,
            set.SetType,
            set.RestSeconds,
            set.DurationSeconds,
            set.DistanceMeters,
            last?.Load,
            last?.Repetitions);
    }
}
