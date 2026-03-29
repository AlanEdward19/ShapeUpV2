using FluentValidation;
using ShapeUp.Features.Training.Exercises.CreateExercise;
using ShapeUp.Features.Training.Exercises.Shared.ViewModels;
using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Documents;
using ShapeUp.Features.Training.Shared.Documents.ValueObjects;
using ShapeUp.Features.Training.Shared.Errors;
using ShapeUp.Features.Training.Workouts.Shared.Dtos;
using ShapeUp.Features.Training.Workouts.Shared.ValueObjects;
using ShapeUp.Features.Training.Workouts.Shared.ViewModels;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Training.Workouts.CreateWorkoutSession;

public class CreateWorkoutSessionHandler(
    IWorkoutSessionRepository workoutSessionRepository,
    IExerciseRepository exerciseRepository,
    ITrainingAccessPolicy accessPolicy,
    IValidator<CreateWorkoutSessionCommand> validator)
{
    public async Task<Result<WorkoutSessionResponse>> HandleAsync(
        CreateWorkoutSessionCommand command,
        int actorUserId,
        string[] actorScopes,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<WorkoutSessionResponse>.Failure(CommonErrors.Validation(string.Join("; ", validation.Errors.Select(x => x.ErrorMessage))));

        var canCreate = await accessPolicy.CanCreateWorkoutForAsync(actorUserId, command.TargetUserId, actorScopes, cancellationToken);
        if (!canCreate)
            return Result<WorkoutSessionResponse>.Failure(TrainingErrors.CannotCreateWorkoutForTarget(actorUserId, command.TargetUserId));

        var exerciseMaps = new List<(ExerciseResponse Exercise, WorkoutExerciseDto Input)>();
        foreach (var exerciseInput in command.Exercises)
        {
            var exercise = await exerciseRepository.GetByIdAsync(exerciseInput.ExerciseId, cancellationToken);
            if (exercise is null)
                return Result<WorkoutSessionResponse>.Failure(TrainingErrors.ExerciseNotFound(exerciseInput.ExerciseId));

            exerciseMaps.Add((CreateExerciseHandler.MapResponse(exercise), exerciseInput));
        }

        var session = new WorkoutSessionDocument
        {
            TargetUserId = command.TargetUserId,
            ExecutedByUserId = command.ExecutedByUserId,
            TrainerUserId = actorUserId == command.TargetUserId ? null : actorUserId,
            StartedAtUtc = command.StartedAtUtc,
            LastSavedAtUtc = command.StartedAtUtc,
            IsCompleted = false,
            Exercises = exerciseMaps
                .Select(x => new ExecutedExerciseDocumentValueObject
                {
                    ExerciseId = x.Exercise.Id,
                    ExerciseName = x.Exercise.Name,
                    Sets = x.Input.Sets
                        .Select(s => new ExecutedSetDocumentValueObject
                        {
                            Repetitions = s.Repetitions,
                            Load = s.Load,
                            LoadUnit = s.LoadUnit.ToLowerInvariant(),
                            SetType = s.SetType.ToLowerInvariant(),
                            Rpe = s.Rpe,
                            RestSeconds = s.RestSeconds,
                            IsExtra = s.IsExtra
                        })
                        .ToList()
                })
                .ToList()
        };

        await workoutSessionRepository.AddAsync(session, cancellationToken);
        return Result<WorkoutSessionResponse>.Success(Map(session));
    }

    internal static WorkoutSessionResponse Map(WorkoutSessionDocument session) =>
        new(
            session.Id,
            session.WorkoutPlanId,
            session.TargetUserId,
            session.ExecutedByUserId,
            session.TrainerUserId,
            session.StartedAtUtc,
            session.LastSavedAtUtc,
            session.EndedAtUtc,
            session.DurationSeconds,
            session.PerceivedExertion,
            session.IsCompleted,
            session.Exercises
                .Select(exercise => new ExecutedExerciseDto(
                    exercise.ExerciseId,
                    exercise.ExerciseName,
                    exercise.Sets.Select(set => new ExecutedSetValueObject(
                        set.Repetitions,
                        set.Load,
                        set.LoadUnit,
                        set.SetType,
                        set.Rpe,
                        set.RestSeconds,
                        set.Volume,
                        set.IsExtra)).ToArray()))
                .ToArray(),
            session.PersonalRecords.Select(pr => new WorkoutPrDto(pr.ExerciseId, pr.ExerciseName, pr.Type, pr.Value)).ToArray());
}