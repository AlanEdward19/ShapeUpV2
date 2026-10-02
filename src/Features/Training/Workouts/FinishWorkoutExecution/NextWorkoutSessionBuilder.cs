using MongoDB.Bson;
using ShapeUp.Features.Training.Shared.Documents;
using ShapeUp.Features.Training.Shared.Documents.ValueObjects;

namespace ShapeUp.Features.Training.Workouts.FinishWorkoutExecution;

/// <summary>
/// Builds the session that follows a finished one: same exercises, order and number of sets as planned, repeating the load
/// the user registered. Never changes exercises, order or volume (that is the paid plan's job).
/// </summary>
public static class NextWorkoutSessionBuilder
{
    /// <param name="finished">The session being closed, with the sets actually done.</param>
    /// <param name="plannedExercises">The session's structure as planned, before the legacy finish list replaced it.</param>
    public static WorkoutSessionDocument Build(WorkoutSessionDocument finished, IReadOnlyList<ExecutedExerciseDocumentValueObject> plannedExercises, DateTime endedAtUtc)
    {
        var exercises = plannedExercises
            .Select(planned => new ExecutedExerciseDocumentValueObject
            {
                ExerciseId = planned.ExerciseId,
                ExerciseName = planned.ExerciseName,
                RequireRpe = planned.RequireRpe,
                ExerciseType = planned.ExerciseType,
                Sets = CopySets(planned, PerformedSets(finished, planned.ExerciseId))
            })
            .ToList();

        return new WorkoutSessionDocument
        {
            Id = ObjectId.GenerateNewId().ToString(),
            WorkoutPlanId = finished.WorkoutPlanId,
            TargetUserId = finished.TargetUserId,
            ExecutedByUserId = finished.ExecutedByUserId,
            TrainerUserId = finished.TrainerUserId,
            // Tomorrow (UTC), so it is not mistaken for a session in progress today.
            StartedAtUtc = endedAtUtc.Date.AddDays(1),
            LastSavedAtUtc = endedAtUtc,
            Exercises = exercises
        };
    }

    private static List<ExecutedSetDocumentValueObject> PerformedSets(WorkoutSessionDocument finished, int exerciseId) =>
        finished.Exercises
            .Where(e => e.ExerciseId == exerciseId)
            .SelectMany(e => e.Sets)
            .Where(s => s.IsPerformed != false && (s.Repetitions is > 0 || s.DurationSeconds is > 0))
            .ToList();

    private static List<ExecutedSetDocumentValueObject> CopySets(ExecutedExerciseDocumentValueObject planned, List<ExecutedSetDocumentValueObject> performed)
    {
        var copies = new List<ExecutedSetDocumentValueObject>();
        // Sets marked via POST .../sets are appended after the plan's prefilled ones, so the plan's own sets define the volume
        // when they exist; otherwise (exercise added or swapped in, legacy finish list) the non-extra sets do.
        var structure = planned.Sets.Where(s => s.IsPerformed == false).ToList();
        if (structure.Count == 0)
            structure = planned.Sets.Where(s => !s.IsExtra).ToList();

        foreach (var set in structure)
        {
            // Set N repeats the load of set N; a set that was not reached repeats the last load registered.
            var source = performed.Count == 0 ? null : performed[Math.Min(copies.Count, performed.Count - 1)];

            copies.Add(new ExecutedSetDocumentValueObject
            {
                Repetitions = set.Repetitions,
                Load = source?.Load ?? set.Load,
                LoadUnit = source?.Load is null ? set.LoadUnit : source.LoadUnit,
                SetType = set.SetType,
                Technique = set.Technique,
                Intensity = set.Intensity is null ? null : new IntensityDocumentValueObject { Type = set.Intensity.Type, Value = set.Intensity.Value },
                RestSeconds = set.RestSeconds,
                DurationSeconds = set.DurationSeconds,
                DistanceMeters = set.DistanceMeters,
                IsExtra = false,
                IsPerformed = false
            });
        }

        return copies;
    }
}
