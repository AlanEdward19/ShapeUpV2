using FluentValidation;
using ShapeUp.Features.Training.Exercises.CreateExercise;
using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Documents.ValueObjects;
using ShapeUp.Features.Training.Shared.Enums;
using ShapeUp.Features.Training.Shared.Errors;
using ShapeUp.Features.Training.Workouts.Shared;
using ShapeUp.Features.Training.Workouts.Shared.ViewModels;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Training.Workouts.MarkWorkoutSet;

public class MarkWorkoutSetHandler(
    IWorkoutSessionRepository workoutSessionRepository,
    IExerciseRepository exerciseRepository,
    IWorkoutSessionResponseMapper workoutSessionResponseMapper,
    IValidator<MarkWorkoutSetCommand> validator)
{
    public async Task<Result<WorkoutSessionResponse>> HandleAsync(MarkWorkoutSetCommand command, int actorUserId, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<WorkoutSessionResponse>.Failure(CommonErrors.Validation(string.Join("; ", validation.Errors.Select(x => x.ErrorMessage))));

        var session = await workoutSessionRepository.GetByIdAsync(command.SessionId, cancellationToken);
        if (session is null)
            return Result<WorkoutSessionResponse>.Failure(TrainingErrors.WorkoutSessionNotFound(command.SessionId));

        if (session.TargetUserId != actorUserId && session.ExecutedByUserId != actorUserId && session.TrainerUserId != actorUserId)
            return Result<WorkoutSessionResponse>.Failure(CommonErrors.Forbidden("You are not allowed to update this workout session."));

        // A retry of an already applied operation is a success and keeps the first payload.
        if (session.AppliedSetOperationIds.Contains(command.OperationId))
            return Result<WorkoutSessionResponse>.Success(workoutSessionResponseMapper.Map(session));

        if (session.IsCompleted)
            return Result<WorkoutSessionResponse>.Failure(TrainingErrors.WorkoutSessionAlreadyCompleted(command.SessionId));

        if (session.IsCancelled)
            return Result<WorkoutSessionResponse>.Failure(TrainingErrors.WorkoutSessionAlreadyCancelled(command.SessionId));

        var exercise = await exerciseRepository.GetByIdAsync(command.ExerciseId, cancellationToken);
        if (exercise is null)
            return Result<WorkoutSessionResponse>.Failure(TrainingErrors.ExerciseNotFound(command.ExerciseId));

        var input = command.Set;

        var requireRpe = session.Exercises.FirstOrDefault(x => x.ExerciseId == command.ExerciseId)?.RequireRpe ?? false;
        if (requireRpe && input.Intensity is null)
            return Result<WorkoutSessionResponse>.Failure(TrainingErrors.RpeRequiredForExercise(command.ExerciseId));

        var mapped = CreateExerciseHandler.MapResponse(exercise);
        if (mapped.ExerciseType == ExerciseType.TimeBased && input.DurationSeconds is null or <= 0)
            return Result<WorkoutSessionResponse>.Failure(TrainingErrors.DurationRequiredForExercise(command.ExerciseId));

        var set = new ExecutedSetDocumentValueObject
        {
            Repetitions = input.Repetitions,
            Load = input.Load,
            LoadUnit = input.LoadUnit,
            SetType = input.SetType,
            Technique = input.Technique,
            Intensity = input.Intensity is null ? null : new IntensityDocumentValueObject { Type = input.Intensity.Type, Value = input.Intensity.Value },
            RestSeconds = input.RestSeconds ?? 0,
            DurationSeconds = input.DurationSeconds,
            DistanceMeters = input.DistanceMeters,
            IsExtra = input.IsExtra
        };

        var exerciseIfMissing = new ExecutedExerciseDocumentValueObject
        {
            ExerciseId = mapped.Id,
            ExerciseName = mapped.Name,
            RequireRpe = false,
            ExerciseType = mapped.ExerciseType
        };

        var applied = await workoutSessionRepository.AppendSetAsync(
            command.SessionId, command.OperationId, exerciseIfMissing, set, DateTime.UtcNow, cancellationToken);

        var updated = await workoutSessionRepository.GetByIdAsync(command.SessionId, cancellationToken);
        if (updated is null)
            return Result<WorkoutSessionResponse>.Failure(TrainingErrors.WorkoutSessionNotFound(command.SessionId));

        if (!applied && !updated.AppliedSetOperationIds.Contains(command.OperationId))
        {
            // Session was finished or cancelled between the read and the write.
            if (updated.IsCompleted)
                return Result<WorkoutSessionResponse>.Failure(TrainingErrors.WorkoutSessionAlreadyCompleted(command.SessionId));
            if (updated.IsCancelled)
                return Result<WorkoutSessionResponse>.Failure(TrainingErrors.WorkoutSessionAlreadyCancelled(command.SessionId));

            return Result<WorkoutSessionResponse>.Failure(CommonErrors.Conflict("The set could not be recorded, please retry."));
        }

        return Result<WorkoutSessionResponse>.Success(workoutSessionResponseMapper.Map(updated));
    }
}
