using System.Globalization;
using FluentValidation;
using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Documents;
using ShapeUp.Features.Training.Shared.Documents.ValueObjects;
using ShapeUp.Features.Training.Shared.Errors;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Training.Workouts.FinishWorkoutExecution;

public class FinishWorkoutExecutionHandler(
    IWorkoutSessionRepository workoutSessionRepository,
    IValidator<FinishWorkoutExecutionCommand> validator)
{
    public async Task<Result> HandleAsync(FinishWorkoutExecutionCommand command, int actorUserId, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result.Failure(CommonErrors.Validation(string.Join("; ", validation.Errors.Select(x => x.ErrorMessage))));

        var session = await workoutSessionRepository.GetByIdAsync(command.SessionId, cancellationToken);
        if (session is null)
            return Result.Failure(TrainingErrors.WorkoutSessionNotFound(command.SessionId));

        if (session.ExecutedByUserId != actorUserId && session.TargetUserId != actorUserId && session.TrainerUserId != actorUserId)
            return Result.Failure(CommonErrors.Forbidden("You are not allowed to finish this workout session."));

        if (session.IsCompleted)
            return Result.Failure(TrainingErrors.WorkoutSessionAlreadyCompleted(command.SessionId));

        if (command.Exercises is not null)
        {
            var mappedExercises = command.Exercises
                .Select(exercise => new ExecutedExerciseDocumentValueObject
                {
                    ExerciseId = exercise.ExerciseId,
                    ExerciseName = session.Exercises.FirstOrDefault(x => x.ExerciseId == exercise.ExerciseId)?.ExerciseName ?? $"Exercise #{exercise.ExerciseId}",
                    Sets = exercise.Sets
                        .Select(set => new ExecutedSetDocumentValueObject
                        {
                            Repetitions = set.Repetitions,
                            Load = set.Load,
                            LoadUnit = set.LoadUnit,
                            SetType = set.SetType,
                            Rpe = set.Rpe,
                            RestSeconds = set.RestSeconds,
                            IsExtra = set.IsExtra
                        })
                        .ToList()
                })
                .ToList();

            await workoutSessionRepository.UpdateStateAsync(command.SessionId, command.EndedAtUtc, mappedExercises, cancellationToken);
            session.Exercises = mappedExercises;
            session.LastSavedAtUtc = command.EndedAtUtc;
        }

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
                prs.Add(new WorkoutPrDocumentValueObject { ExerciseId = exercise.ExerciseId, ExerciseName = exercise.ExerciseName, Type = "max_volume", Value = currentMaxVolume });

            var historicalMaxLoad = historySets.Select(s => s.Load).DefaultIfEmpty(0m).Max();
            var currentMaxLoad = exercise.Sets.Select(s => s.Load).DefaultIfEmpty(0m).Max();
            if (currentMaxLoad > historicalMaxLoad)
                prs.Add(new WorkoutPrDocumentValueObject { ExerciseId = exercise.ExerciseId, ExerciseName = exercise.ExerciseName, Type = "max_load", Value = currentMaxLoad });

            var repsByLoadHistory = historySets
                .GroupBy(x => x.Load)
                .ToDictionary(x => x.Key, x => x.Max(s => s.Repetitions));

            foreach (var groupedSet in exercise.Sets.GroupBy(x => x.Load))
            {
                var currentBestRep = groupedSet.Max(x => x.Repetitions);
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
        }

        return prs;
    }
}

