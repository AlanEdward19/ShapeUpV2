using FluentValidation;
using ShapeUp.Features.Training.Exercises.CreateExercise;
using ShapeUp.Features.Training.Exercises.Shared.ViewModels;
using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Documents.ValueObjects;
using ShapeUp.Features.Training.Shared.Errors;
using ShapeUp.Features.Training.Workouts.CreateWorkoutSession;
using ShapeUp.Features.Training.Workouts.Shared.Dtos;
using ShapeUp.Features.Training.Workouts.Shared.ViewModels;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Training.Workouts.UpdateWorkoutExecutionState;

public class UpdateWorkoutExecutionStateHandler(
    IWorkoutSessionRepository workoutSessionRepository,
    IExerciseRepository exerciseRepository,
    IValidator<UpdateWorkoutExecutionStateCommand> validator)
{
    public async Task<Result<WorkoutSessionResponse>> HandleAsync(UpdateWorkoutExecutionStateCommand command, int actorUserId, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<WorkoutSessionResponse>.Failure(CommonErrors.Validation(string.Join("; ", validation.Errors.Select(x => x.ErrorMessage))));

        var session = await workoutSessionRepository.GetByIdAsync(command.SessionId, cancellationToken);
        if (session is null)
            return Result<WorkoutSessionResponse>.Failure(TrainingErrors.WorkoutSessionNotFound(command.SessionId));

        if (session.TargetUserId != actorUserId && session.ExecutedByUserId != actorUserId && session.TrainerUserId != actorUserId)
            return Result<WorkoutSessionResponse>.Failure(CommonErrors.Forbidden("You are not allowed to update this workout session."));

        if (session.IsCompleted)
            return Result<WorkoutSessionResponse>.Failure(TrainingErrors.WorkoutSessionAlreadyCompleted(command.SessionId));

        var exerciseMaps = new List<(ExerciseResponse Exercise, WorkoutExerciseDto Input)>();
        foreach (var exerciseInput in command.Exercises)
        {
            var exercise = await exerciseRepository.GetByIdAsync(exerciseInput.ExerciseId, cancellationToken);
            if (exercise is null)
                return Result<WorkoutSessionResponse>.Failure(TrainingErrors.ExerciseNotFound(exerciseInput.ExerciseId));

            exerciseMaps.Add((CreateExerciseHandler.MapResponse(exercise), exerciseInput));
        }

        var mappedExercises = exerciseMaps
            .Select(x => new ExecutedExerciseDocumentValueObject
            {
                ExerciseId = x.Exercise.Id,
                ExerciseName = x.Exercise.Name,
                Sets = x.Input.Sets
                    .Select(s => new ExecutedSetDocumentValueObject
                    {
                        Repetitions = s.Repetitions,
                        Load = s.Load,
                        LoadUnit = s.LoadUnit,
                        SetType = s.SetType,
                        Rpe = s.Rpe,
                        RestSeconds = s.RestSeconds,
                        IsExtra = s.IsExtra
                    })
                    .ToList()
            })
            .ToList();

        await workoutSessionRepository.UpdateStateAsync(command.SessionId, command.SavedAtUtc, mappedExercises, cancellationToken);
        session.Exercises = mappedExercises;
        session.LastSavedAtUtc = command.SavedAtUtc;

        return Result<WorkoutSessionResponse>.Success(CreateWorkoutSessionHandler.Map(session));
    }
}

