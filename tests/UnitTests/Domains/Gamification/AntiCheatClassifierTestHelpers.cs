using ShapeUp.Features.Training.Shared.Documents;
using ShapeUp.Features.Training.Shared.Documents.ValueObjects;

namespace UnitTests.Domains.Gamification;

internal static class AntiCheatClassifierTestHelpers
{
    internal static WorkoutSessionDocument CreateSession(
        string id,
        int userId,
        DateTime endedAtUtc,
        int durationSeconds,
        IReadOnlyList<(int exerciseId, int reps, decimal load, int restSeconds)> sets)
    {
        var exercises = sets
            .GroupBy(s => s.exerciseId)
            .Select(group => new ExecutedExerciseDocumentValueObject
            {
                ExerciseId = group.Key,
                ExerciseName = $"Exercise {group.Key}",
                Sets = group.Select(set => new ExecutedSetDocumentValueObject
                {
                    Repetitions = set.reps,
                    Load = set.load,
                    RestSeconds = set.restSeconds
                }).ToList()
            })
            .ToList();

        return new WorkoutSessionDocument
        {
            Id = id,
            ExecutedByUserId = userId,
            EndedAtUtc = endedAtUtc,
            DurationSeconds = durationSeconds,
            IsCompleted = true,
            Exercises = exercises
        };
    }

    internal static WorkoutSessionDocument CreateUniformVolumeSession(
        string id,
        int userId,
        DateTime endedAtUtc,
        int durationSeconds,
        decimal targetVolume,
        int restSeconds = 90)
    {
        var reps = 10;
        var load = targetVolume / reps;
        return CreateSession(
            id,
            userId,
            endedAtUtc,
            durationSeconds,
            [(1, reps, load, restSeconds)]);
    }

    internal static WorkoutSessionDocument CreateDurationRatioSession(
        string id,
        int userId,
        DateTime endedAtUtc,
        double ratio,
        int restSeconds = 90,
        int setCount = 1)
    {
        var expectedMinimumSeconds = setCount * (restSeconds + 10);
        var durationSeconds = (int)Math.Round(ratio * expectedMinimumSeconds);
        var sets = Enumerable.Range(0, setCount)
            .Select(_ => (1, 10, 10m, restSeconds))
            .ToList();

        return CreateSession(id, userId, endedAtUtc, durationSeconds, sets);
    }

    internal static IReadOnlyList<WorkoutSessionDocument> CreateVolumeBaseline(
        int userId,
        DateTime currentEndedAtUtc,
        int count,
        decimal volumePerSession = 100m)
    {
        return Enumerable.Range(1, count)
            .Select(index => CreateUniformVolumeSession(
                $"prior-{index}",
                userId,
                currentEndedAtUtc.AddDays(-index),
                durationSeconds: 120,
                targetVolume: volumePerSession))
            .ToList();
    }
}
