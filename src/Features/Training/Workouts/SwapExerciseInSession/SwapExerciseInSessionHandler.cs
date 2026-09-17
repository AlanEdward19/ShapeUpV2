using FluentValidation;
using ShapeUp.Features.Training.Exercises.CreateExercise;
using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Documents.ValueObjects;
using ShapeUp.Features.Training.Shared.Errors;
using ShapeUp.Features.Training.Workouts.Shared;
using ShapeUp.Features.Training.Workouts.Shared.ViewModels;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Training.Workouts.SwapExerciseInSession;

public class SwapExerciseInSessionHandler(
    IWorkoutSessionRepository workoutSessionRepository,
    IExerciseRepository exerciseRepository,
    IExerciseEquivalentRepository equivalentRepository,
    IWorkoutSessionResponseMapper workoutSessionResponseMapper,
    IValidator<SwapExerciseInSessionCommand> validator)
{
    public async Task<Result<WorkoutSessionResponse>> HandleAsync(
        SwapExerciseInSessionCommand command,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<WorkoutSessionResponse>.Failure(
                CommonErrors.Validation(string.Join("; ", validation.Errors.Select(x => x.ErrorMessage))));

        var session = await workoutSessionRepository.GetByIdAsync(command.SessionId, cancellationToken);
        if (session is null)
            return Result<WorkoutSessionResponse>.Failure(TrainingErrors.WorkoutSessionNotFound(command.SessionId));

        if (session.TargetUserId != actorUserId && session.ExecutedByUserId != actorUserId && session.TrainerUserId != actorUserId)
            return Result<WorkoutSessionResponse>.Failure(CommonErrors.Forbidden("You are not allowed to update this workout session."));

        if (session.IsCompleted)
            return Result<WorkoutSessionResponse>.Failure(TrainingErrors.WorkoutSessionAlreadyCompleted(command.SessionId));

        if (session.IsCancelled)
            return Result<WorkoutSessionResponse>.Failure(TrainingErrors.WorkoutSessionAlreadyCancelled(command.SessionId));

        var original = session.Exercises.FirstOrDefault(x => x.ExerciseId == command.OriginalExerciseId);
        if (original is null)
            return Result<WorkoutSessionResponse>.Failure(TrainingErrors.OriginalExerciseNotInSession(command.OriginalExerciseId));

        if (session.Exercises.Any(x => x.ExerciseId == command.NewExerciseId))
            return Result<WorkoutSessionResponse>.Failure(TrainingErrors.ExerciseAlreadyInSession(command.NewExerciseId));

        var equivalents = await equivalentRepository.GetEquivalentsAsync(command.OriginalExerciseId, cancellationToken);
        if (equivalents.All(x => x.Id != command.NewExerciseId))
            return Result<WorkoutSessionResponse>.Failure(
                TrainingErrors.ExercisesNotEquivalent(command.OriginalExerciseId, command.NewExerciseId));

        var newExercise = await exerciseRepository.GetByIdAsync(command.NewExerciseId, cancellationToken);
        if (newExercise is null)
            return Result<WorkoutSessionResponse>.Failure(TrainingErrors.ExerciseNotFound(command.NewExerciseId));

        original.Sets = command.RetainedSetsForOriginal
            .Select(s => new ExecutedSetDocumentValueObject
            {
                Repetitions = s.Repetitions ?? 0,
                Load = s.Load,
                LoadUnit = s.LoadUnit,
                SetType = s.SetType,
                Technique = s.Technique,
                Intensity = s.Intensity is null ? null : new IntensityDocumentValueObject { Type = s.Intensity.Type, Value = s.Intensity.Value },
                RestSeconds = s.RestSeconds ?? 0,
                IsExtra = s.IsExtra
            })
            .ToList();

        var mapped = CreateExerciseHandler.MapResponse(newExercise);
        session.Exercises.Add(new ExecutedExerciseDocumentValueObject
        {
            ExerciseId = mapped.Id,
            ExerciseName = mapped.Name,
            RequireRpe = false,
            Sets = []
        });

        var savedAtUtc = DateTime.UtcNow;
        await workoutSessionRepository.UpdateStateAsync(command.SessionId, savedAtUtc, session.Exercises, cancellationToken);
        session.LastSavedAtUtc = savedAtUtc;

        return Result<WorkoutSessionResponse>.Success(workoutSessionResponseMapper.Map(session));
    }
}
