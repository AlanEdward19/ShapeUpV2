using ShapeUp.Features.Gamification.Shared.Enums;
using ShapeUp.Features.Training.Shared.Documents;
using ShapeUp.Features.Training.Shared.Documents.ValueObjects;

namespace ShapeUp.Features.Gamification.Shared.AntiCheat;

public sealed class AntiCheatClassifier : IAntiCheatClassifier
{
    private static readonly TimeSpan DuplicationWindow = TimeSpan.FromMinutes(5);

    public Task<AntiCheatResult> ClassifyAsync(
        WorkoutSessionDocument session,
        IReadOnlyList<WorkoutSessionDocument> recentSessions,
        CancellationToken cancellationToken = default)
    {
        var priorSessions = recentSessions
            .Where(s => s.Id != session.Id)
            .ToList();

        var durationVote = ClassifyDuration(session);
        var duplicationVote = ClassifyDuplication(session, priorSessions);
        var volumeVote = ClassifyVolume(session, priorSessions);

        var winningVote = new[] { durationVote, duplicationVote, volumeVote }
            .OrderByDescending(v => (int)v.Classification)
            .First();

        return Task.FromResult(new AntiCheatResult(winningVote.Classification, winningVote.Reason));
    }

    internal static (ActivityClassification Classification, string Reason) ClassifyDuration(WorkoutSessionDocument session)
    {
        if (session.Exercises.Count == 0)
        {
            return (ActivityClassification.Invalid, "Duration: no exercises recorded");
        }

        var setCount = session.Exercises.Sum(e => e.Sets.Count);
        if (setCount == 0)
        {
            return (ActivityClassification.Invalid, "Duration: no sets recorded");
        }

        if (session.DurationSeconds is null or <= 0)
        {
            return (ActivityClassification.Invalid, "Duration: missing or zero duration");
        }

        var restSeconds = session.Exercises.Sum(e => e.Sets.Sum(s => s.RestSeconds));
        var minimumExecutionSeconds = setCount * 10;
        var expectedMinimumSeconds = restSeconds + minimumExecutionSeconds;

        if (expectedMinimumSeconds <= 0)
        {
            return (ActivityClassification.Invalid, "Duration: zero expected minimum time");
        }

        var ratio = session.DurationSeconds.Value / (double)expectedMinimumSeconds;

        if (ratio < 0.3)
        {
            return (ActivityClassification.Invalid, "Duration: ratio below 0.3");
        }

        if (ratio < 0.5)
        {
            return (ActivityClassification.Suspicious, "Duration: ratio between 0.3 and 0.5");
        }

        if (ratio < 0.8)
        {
            return (ActivityClassification.LikelyValid, "Duration: ratio between 0.5 and 0.8");
        }

        return (ActivityClassification.Verified, "Duration: ratio at or above 0.8");
    }

    internal static (ActivityClassification Classification, string Reason) ClassifyDuplication(
        WorkoutSessionDocument session,
        IReadOnlyList<WorkoutSessionDocument> priorSessions)
    {
        if (session.EndedAtUtc is null)
        {
            return (ActivityClassification.Verified, "Duplication: session not finished");
        }

        var candidates = priorSessions
            .Where(s => s.EndedAtUtc is not null
                        && s.ExecutedByUserId == session.ExecutedByUserId
                        && s.EndedAtUtc <= session.EndedAtUtc
                        && session.EndedAtUtc - s.EndedAtUtc <= DuplicationWindow)
            .ToList();

        if (candidates.Count == 0)
        {
            return (ActivityClassification.Verified, "Duplication: no recent prior sessions");
        }

        var worstVote = (Classification: ActivityClassification.Verified, Reason: "Duplication: no match", MatchRatio: 0.0);

        foreach (var prior in candidates)
        {
            if (IsExactDuplicate(session, prior))
            {
                return (ActivityClassification.Invalid, "Duplication: exact replay match");
            }

            var matchRatio = ComputePairMatchRatio(session, prior);
            if (matchRatio > worstVote.MatchRatio)
            {
                worstVote = (
                    matchRatio >= 0.8
                        ? ActivityClassification.Suspicious
                        : ActivityClassification.Verified,
                    matchRatio >= 0.8
                        ? "Duplication: partial replay match"
                        : "Duplication: no significant match",
                    matchRatio);
            }
        }

        return (worstVote.Classification, worstVote.Reason);
    }

    internal static (ActivityClassification Classification, string Reason) ClassifyVolume(
        WorkoutSessionDocument session,
        IReadOnlyList<WorkoutSessionDocument> priorSessions)
    {
        var baselineSessions = priorSessions
            .OrderByDescending(s => s.EndedAtUtc)
            .Take(10)
            .ToList();

        if (baselineSessions.Count < 3)
        {
            return (ActivityClassification.LikelyValid, "Volume: insufficient prior session history");
        }

        var baselineAverage = baselineSessions.Average(GetSessionVolume);
        var currentVolume = GetSessionVolume(session);

        if (baselineAverage <= 0)
        {
            return currentVolume > 0
                ? (ActivityClassification.Invalid, "Volume: spike above zero baseline")
                : (ActivityClassification.Verified, "Volume: within baseline");
        }

        var multiplier = currentVolume / baselineAverage;

        if (multiplier > 6)
        {
            return (ActivityClassification.Invalid, "Volume: above 6x baseline average");
        }

        if (multiplier > 3)
        {
            return (ActivityClassification.Suspicious, "Volume: between 3x and 6x baseline average");
        }

        return (ActivityClassification.Verified, "Volume: within baseline");
    }

    private static decimal GetSessionVolume(WorkoutSessionDocument session) =>
        session.Exercises.SelectMany(e => e.Sets).Sum(s => s.Volume);

    private static bool IsExactDuplicate(WorkoutSessionDocument current, WorkoutSessionDocument prior)
    {
        if (current.Exercises.Count != prior.Exercises.Count)
        {
            return false;
        }

        for (var exerciseIndex = 0; exerciseIndex < current.Exercises.Count; exerciseIndex++)
        {
            var currentExercise = current.Exercises[exerciseIndex];
            var priorExercise = prior.Exercises[exerciseIndex];

            if (currentExercise.ExerciseId != priorExercise.ExerciseId
                || currentExercise.Sets.Count != priorExercise.Sets.Count)
            {
                return false;
            }

            for (var setIndex = 0; setIndex < currentExercise.Sets.Count; setIndex++)
            {
                var currentSet = currentExercise.Sets[setIndex];
                var priorSet = priorExercise.Sets[setIndex];

                if (currentSet.Load != priorSet.Load || currentSet.Repetitions != priorSet.Repetitions)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static double ComputePairMatchRatio(WorkoutSessionDocument current, WorkoutSessionDocument prior)
    {
        var currentPairs = FlattenPairs(current);
        if (currentPairs.Count == 0)
        {
            return 0;
        }

        var priorPairs = FlattenPairs(prior).ToHashSet();
        var matches = currentPairs.Count(priorPairs.Contains);
        return matches / (double)currentPairs.Count;
    }

    private static List<(int ExerciseId, decimal Load, int Repetitions)> FlattenPairs(WorkoutSessionDocument session) =>
        session.Exercises
            .SelectMany(exercise => exercise.Sets.Select(set => (exercise.ExerciseId, set.Load, set.Repetitions)))
            .ToList();
}
