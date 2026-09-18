using FluentValidation;
using ShapeUp.Features.Training.Exercises.CreateExercise;
using ShapeUp.Features.Training.Exercises.Shared.ViewModels;
using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Documents.ValueObjects;
using ShapeUp.Features.Training.Shared.Enums;
using ShapeUp.Features.Training.Shared.Errors;
using ShapeUp.Features.Training.Workouts.Shared;
using ShapeUp.Features.Training.Workouts.Shared.Dtos;
using ShapeUp.Features.Training.Workouts.Shared.ViewModels;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Training.Workouts.UpdateWorkoutExecutionState;

public class UpdateWorkoutExecutionStateHandler(
    IWorkoutSessionRepository workoutSessionRepository,
    IExerciseRepository exerciseRepository,
    IWorkoutSessionResponseMapper workoutSessionResponseMapper,
    IValidator<UpdateWorkoutExecutionStateCommand> validator)
{
    public async Task<Result<WorkoutSessionResponse>> HandleAsync(UpdateWorkoutExecutionStateCommand command, int actorUserId, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<WorkoutSessionResponse>.Failure(CommonErrors.Validation(string.Join("; ", validation.Errors.Select(x => x.ErrorMessage))));

        var savedAtUtc = command.SavedAtUtc!.Value;

        var session = await workoutSessionRepository.GetByIdAsync(command.SessionId, cancellationToken);
        if (session is null)
            return Result<WorkoutSessionResponse>.Failure(TrainingErrors.WorkoutSessionNotFound(command.SessionId));

        if (session.TargetUserId != actorUserId && session.ExecutedByUserId != actorUserId && session.TrainerUserId != actorUserId)
            return Result<WorkoutSessionResponse>.Failure(CommonErrors.Forbidden("You are not allowed to update this workout session."));

        if (session.IsCompleted)
            return Result<WorkoutSessionResponse>.Failure(TrainingErrors.WorkoutSessionAlreadyCompleted(command.SessionId));

        if (session.IsCancelled)
            return Result<WorkoutSessionResponse>.Failure(TrainingErrors.WorkoutSessionAlreadyCancelled(command.SessionId));

        var exerciseMaps = new List<(ExerciseResponse Exercise, WorkoutExerciseDto Input, bool RequireRpe)>();
        foreach (var exerciseInput in command.Exercises)
        {
            var exercise = await exerciseRepository.GetByIdAsync(exerciseInput.ExerciseId, cancellationToken);
            if (exercise is null)
                return Result<WorkoutSessionResponse>.Failure(TrainingErrors.ExerciseNotFound(exerciseInput.ExerciseId));

            var requireRpe = session.Exercises.FirstOrDefault(x => x.ExerciseId == exerciseInput.ExerciseId)?.RequireRpe ?? false;
            if (requireRpe && exerciseInput.Sets.Any(s => s.Intensity is null))
                return Result<WorkoutSessionResponse>.Failure(TrainingErrors.RpeRequiredForExercise(exerciseInput.ExerciseId));

            var mapped = CreateExerciseHandler.MapResponse(exercise);
            if (mapped.ExerciseType == ExerciseType.TimeBased && exerciseInput.Sets.Any(s => s.DurationSeconds is null || s.DurationSeconds <= 0))
                return Result<WorkoutSessionResponse>.Failure(TrainingErrors.DurationRequiredForExercise(exerciseInput.ExerciseId));

            exerciseMaps.Add((mapped, exerciseInput, requireRpe));
        }

        var mappedExercises = exerciseMaps
            .Select(x => new ExecutedExerciseDocumentValueObject
            {
                ExerciseId = x.Exercise.Id,
                ExerciseName = x.Exercise.Name,
                RequireRpe = x.RequireRpe,
                // SPEC_DEVIATION: task T13 only listed DurationSeconds/DistanceMeters/nullable-Repetitions
                // in scope, but this Select rebuilds ExecutedExerciseDocumentValueObject from scratch on
                // every state update, so ExerciseType would silently reset to the default (WeightBased)
                // without this line -- undoing T12's flatten-at-start and breaking the ExerciseType read
                // that Phase 5 (T20/T21 PR evaluation) explicitly depends on. Setting it here keeps this
                // handler within Phase 3's own stated mandate ("just don't lose data").
                ExerciseType = x.Exercise.ExerciseType,
                Sets = x.Input.Sets
                    .Select(s => new ExecutedSetDocumentValueObject
                    {
                        Repetitions = s.Repetitions,
                        Load = s.Load,
                        LoadUnit = s.LoadUnit,
                        SetType = s.SetType,
                        Technique = s.Technique,
                        Intensity = s.Intensity is null ? null : new IntensityDocumentValueObject { Type = s.Intensity.Type, Value = s.Intensity.Value },
                        // RestSeconds!.Value assumed non-null under the old unconditional DTO
                        // validator rule; T14 relaxed that rule to allow a TimeBased set (Load
                        // null) through with RestSeconds null, so the force-unwrap here would throw
                        // for a legitimate TimeBased request. Same "?? 0" safe-default already used
                        // for this exact field in FinishWorkoutExecutionHandler.
                        RestSeconds = s.RestSeconds ?? 0,
                        DurationSeconds = s.DurationSeconds,
                        DistanceMeters = s.DistanceMeters,
                        IsExtra = s.IsExtra
                    })
                    .ToList()
            })
            .ToList();

        await workoutSessionRepository.UpdateStateAsync(command.SessionId, savedAtUtc, mappedExercises, cancellationToken);
        session.Exercises = mappedExercises;
        session.LastSavedAtUtc = savedAtUtc;

        return Result<WorkoutSessionResponse>.Success(workoutSessionResponseMapper.Map(session));
    }
}

