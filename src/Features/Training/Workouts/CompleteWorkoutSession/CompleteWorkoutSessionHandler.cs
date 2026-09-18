using System.Globalization;
using FluentValidation;
using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Documents;
using ShapeUp.Features.Training.Shared.Documents.ValueObjects;
using ShapeUp.Features.Training.Shared.Enums;
using ShapeUp.Features.Training.Shared.Errors;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Training.Workouts.CompleteWorkoutSession;

public class CompleteWorkoutSessionHandler(
    IWorkoutSessionRepository workoutSessionRepository,
    IValidator<CompleteWorkoutSessionCommand> validator)
{
    public async Task<Result> HandleAsync(CompleteWorkoutSessionCommand command, int actorUserId, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result.Failure(CommonErrors.Validation(string.Join("; ", validation.Errors.Select(x => x.ErrorMessage))));

        var session = await workoutSessionRepository.GetByIdAsync(command.SessionId, cancellationToken);
        if (session is null)
            return Result.Failure(TrainingErrors.WorkoutSessionNotFound(command.SessionId));

        if (session.ExecutedByUserId != actorUserId && session.TargetUserId != actorUserId)
            return Result.Failure(CommonErrors.Forbidden("You are not allowed to complete this workout session."));

        if (session.IsCompleted)
            return Result.Failure(TrainingErrors.WorkoutSessionAlreadyCompleted(command.SessionId));

        var history = await workoutSessionRepository.GetCompletedByUserInRangeAsync(session.TargetUserId, new DateTime(2000, 1, 1), command.EndedAtUtc, cancellationToken);

        var personalRecords = EvaluatePrs(session, history);
        await workoutSessionRepository.UpdateCompletionAsync(command.SessionId, command.EndedAtUtc, command.PerceivedExertion, personalRecords, cancellationToken);

        return Result.Success();
    }

    private static List<WorkoutPrDocumentValueObject> EvaluatePrs(WorkoutSessionDocument currentSession, IReadOnlyList<WorkoutSessionDocument> history)
    {
        var prs = new List<WorkoutPrDocumentValueObject>();

        foreach (var exercise in currentSession.Exercises)
        {
            var historySets = history
                .SelectMany(session => session.Exercises)
                .Where(e => e.ExerciseId == exercise.ExerciseId)
                .SelectMany(e => e.Sets)
                .ToArray();

            var historicalMaxVolume = historySets.Select(s => s.Volume).DefaultIfEmpty(0m).Max();
            var currentMaxVolume = exercise.Sets.Select(s => s.Volume).DefaultIfEmpty(0m).Max();
            if (currentMaxVolume > historicalMaxVolume)
                prs.Add(new WorkoutPrDocumentValueObject() { ExerciseId = exercise.ExerciseId, ExerciseName = exercise.ExerciseName, Type = "max_volume", Value = currentMaxVolume });

            // TimeBased sets have no Load -- exclude them from max_load/max_reps_same_load so a
            // null Load never flows into these numeric comparisons/groupings (they should never
            // surface a load-based PR; see TBE-06).
            var weightBasedHistorySets = historySets.Where(s => s.Load.HasValue).ToArray();
            var weightBasedCurrentSets = exercise.Sets.Where(s => s.Load.HasValue).ToArray();

            var historicalMaxLoad = weightBasedHistorySets.Select(s => s.Load!.Value).DefaultIfEmpty(0m).Max();
            var currentMaxLoad = weightBasedCurrentSets.Select(s => s.Load!.Value).DefaultIfEmpty(0m).Max();
            if (currentMaxLoad > historicalMaxLoad)
                prs.Add(new WorkoutPrDocumentValueObject() { ExerciseId = exercise.ExerciseId, ExerciseName = exercise.ExerciseName, Type = "max_load", Value = currentMaxLoad });

            var repsByLoadHistory = weightBasedHistorySets
                .GroupBy(x => x.Load!.Value)
                .ToDictionary(x => x.Key, x => x.Max(s => s.Repetitions ?? 0));

            foreach (var groupedSet in weightBasedCurrentSets.GroupBy(x => x.Load!.Value))
            {
                var currentBestRep = groupedSet.Max(x => x.Repetitions ?? 0);
                var historicalBestRep = repsByLoadHistory.TryGetValue(groupedSet.Key, out var rep) ? rep : 0;

                if (currentBestRep > historicalBestRep)
                {
                    prs.Add(new WorkoutPrDocumentValueObject()
                    {
                        ExerciseId = exercise.ExerciseId,
                        ExerciseName = exercise.ExerciseName,
                        Type = $"max_reps_same_load:{groupedSet.Key.ToString(CultureInfo.InvariantCulture)}",
                        Value = currentBestRep
                    });
                }
            }

            // best_pace (TBE-06): only for TimeBased exercises, only for sets with a distance
            // recorded (DistanceMeters > 0) -- a set without distance (e.g. stretching) is never
            // considered, per spec, not an error.
            if (exercise.ExerciseType == ExerciseType.TimeBased)
            {
                var pacedCurrentSets = exercise.Sets
                    .Where(s => s.DurationSeconds is >= 1 && s.DistanceMeters is > 0)
                    .ToArray();

                if (pacedCurrentSets.Length > 0)
                {
                    var pacedHistorySets = historySets
                        .Where(s => s.DurationSeconds is >= 1 && s.DistanceMeters is > 0)
                        .ToArray();

                    var historicalBestPace = pacedHistorySets
                        .Select(s => s.DurationSeconds!.Value / s.DistanceMeters!.Value)
                        .DefaultIfEmpty(decimal.MaxValue)
                        .Min();

                    var currentBestPace = pacedCurrentSets
                        .Select(s => s.DurationSeconds!.Value / s.DistanceMeters!.Value)
                        .Min();

                    if (currentBestPace < historicalBestPace)
                        prs.Add(new WorkoutPrDocumentValueObject { ExerciseId = exercise.ExerciseId, ExerciseName = exercise.ExerciseName, Type = "best_pace", Value = currentBestPace });
                }
            }
        }

        return prs;
    }
}