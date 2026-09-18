using System.Globalization;
using FluentValidation;
using MassTransit;
using ShapeUp.Configurations;
using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Documents;
using ShapeUp.Features.Training.Shared.Documents.ValueObjects;
using ShapeUp.Features.Training.Shared.Enums;
using ShapeUp.Features.Training.Shared.Errors;
using ShapeUp.Features.Training.Shared.Events;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Training.Workouts.FinishWorkoutExecution;

public class FinishWorkoutExecutionHandler(
    IWorkoutSessionRepository workoutSessionRepository,
    IValidator<FinishWorkoutExecutionCommand> validator,
    IPublishEndpoint publishEndpoint,
    IWorkoutOutboxTransaction outboxTransaction,
    IOutboxFaultInjector outboxFaultInjector)
{
    public async Task<Result> HandleAsync(FinishWorkoutExecutionCommand command, int actorUserId, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result.Failure(CommonErrors.Validation(string.Join("; ", validation.Errors.Select(x => x.ErrorMessage))));

        var endedAtUtc = command.EndedAtUtc!.Value;

        var session = await workoutSessionRepository.GetByIdAsync(command.SessionId, cancellationToken);
        if (session is null)
            return Result.Failure(TrainingErrors.WorkoutSessionNotFound(command.SessionId));

        if (session.ExecutedByUserId != actorUserId && session.TargetUserId != actorUserId && session.TrainerUserId != actorUserId)
            return Result.Failure(CommonErrors.Forbidden("You are not allowed to finish this workout session."));

        if (session.IsCompleted)
            return Result.Failure(TrainingErrors.WorkoutSessionAlreadyCompleted(command.SessionId));

        if (session.IsCancelled)
            return Result.Failure(TrainingErrors.WorkoutSessionAlreadyCancelled(command.SessionId));

        if (command.Exercises is not null)
        {
            foreach (var exercise in command.Exercises)
            {
                var requireRpe = session.Exercises.FirstOrDefault(x => x.ExerciseId == exercise.ExerciseId)?.RequireRpe ?? false;
                if (requireRpe && exercise.Sets.Any(s => s.Intensity is null))
                    return Result.Failure(TrainingErrors.RpeRequiredForExercise(exercise.ExerciseId));
            }

            var mappedExercises = command.Exercises
                .Select(exercise => new ExecutedExerciseDocumentValueObject
                {
                    ExerciseId = exercise.ExerciseId,
                    ExerciseName = session.Exercises.FirstOrDefault(x => x.ExerciseId == exercise.ExerciseId)?.ExerciseName ?? $"Exercise #{exercise.ExerciseId}",
                    RequireRpe = session.Exercises.FirstOrDefault(x => x.ExerciseId == exercise.ExerciseId)?.RequireRpe ?? false,
                    // SPEC_DEVIATION: this projection rebuilds ExecutedExerciseDocumentValueObject from
                    // scratch and, before this task, never copied ExerciseType/DurationSeconds/
                    // DistanceMeters -- meaning a TimeBased exercise finished via command.Exercises would
                    // silently reset to WeightBased and lose its duration/distance, making EvaluatePrs
                    // below (this task's own best_pace logic) unable to detect it. Same class of gap T13
                    // fixed in UpdateWorkoutExecutionStateHandler; fixed here for the same reason.
                    ExerciseType = session.Exercises.FirstOrDefault(x => x.ExerciseId == exercise.ExerciseId)?.ExerciseType ?? ExerciseType.WeightBased,
                    Sets = exercise.Sets
                        .Select(set => new ExecutedSetDocumentValueObject
                        {
                            Repetitions = set.Repetitions ?? 0,
                            Load = set.Load,
                            LoadUnit = set.LoadUnit,
                            SetType = set.SetType,
                            Intensity = set.Intensity is null ? null : new IntensityDocumentValueObject { Type = set.Intensity.Type, Value = set.Intensity.Value },
                            RestSeconds = set.RestSeconds ?? 0,
                            DurationSeconds = set.DurationSeconds,
                            DistanceMeters = set.DistanceMeters,
                            IsExtra = set.IsExtra
                        })
                        .ToList()
                })
                .ToList();

            await workoutSessionRepository.UpdateStateAsync(command.SessionId, endedAtUtc, mappedExercises, cancellationToken);
            session.Exercises = mappedExercises;
            session.LastSavedAtUtc = endedAtUtc;
        }

        var history = await workoutSessionRepository.GetCompletedByUserInRangeAsync(session.TargetUserId, new DateTime(2000, 1, 1), endedAtUtc, cancellationToken);
        var personalRecords = EvaluatePrs(session, history);

        await outboxTransaction.ExecuteAsync(async (mongoSession, ct) =>
        {
            await workoutSessionRepository.UpdateCompletionAsync(
                command.SessionId,
                endedAtUtc,
                command.PerceivedExertion,
                personalRecords,
                ct,
                mongoSession);

            await publishEndpoint.Publish(
                new WorkoutFinished(session.Id, session.TargetUserId, session.ExecutedByUserId, endedAtUtc),
                ct);

            await outboxFaultInjector.AfterPublishAsync(ct);
        }, cancellationToken);

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
                prs.Add(new WorkoutPrDocumentValueObject { ExerciseId = exercise.ExerciseId, ExerciseName = exercise.ExerciseName, Type = "max_volume", Value = currentMaxVolume });

            // TimeBased sets have no Load -- exclude them from max_load/max_reps_same_load so a
            // null Load never flows into these numeric comparisons/groupings (they should never
            // surface a load-based PR; see TBE-06).
            var weightBasedHistorySets = historySets.Where(s => s.Load.HasValue).ToArray();
            var weightBasedCurrentSets = exercise.Sets.Where(s => s.Load.HasValue).ToArray();

            var historicalMaxLoad = weightBasedHistorySets.Select(s => s.Load!.Value).DefaultIfEmpty(0m).Max();
            var currentMaxLoad = weightBasedCurrentSets.Select(s => s.Load!.Value).DefaultIfEmpty(0m).Max();
            if (currentMaxLoad > historicalMaxLoad)
                prs.Add(new WorkoutPrDocumentValueObject { ExerciseId = exercise.ExerciseId, ExerciseName = exercise.ExerciseName, Type = "max_load", Value = currentMaxLoad });

            var repsByLoadHistory = weightBasedHistorySets
                .GroupBy(x => x.Load!.Value)
                .ToDictionary(x => x.Key, x => x.Max(s => s.Repetitions ?? 0));

            foreach (var groupedSet in weightBasedCurrentSets.GroupBy(x => x.Load!.Value))
            {
                var currentBestRep = groupedSet.Max(x => x.Repetitions ?? 0);
                var historicalBestRep = repsByLoadHistory.TryGetValue(groupedSet.Key, out var rep) ? rep : 0;

                if (currentBestRep > historicalBestRep)
                {
                    prs.Add(new WorkoutPrDocumentValueObject
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
