using FluentValidation;
using ShapeUp.Features.Training.Exercises.CreateExercise;
using ShapeUp.Features.Training.Exercises.Shared.ViewModels;
using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Documents;
using ShapeUp.Features.Training.Shared.Documents.ValueObjects;
using ShapeUp.Features.Training.Shared.Errors;
using ShapeUp.Features.Training.WorkoutPlans.Shared;
using ShapeUp.Features.Training.WorkoutPlans.Shared.ViewModels;
using ShapeUp.Features.Training.Workouts.Shared.Dtos;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Training.WorkoutPlans.CreateWorkoutPlan;

public class CreateWorkoutPlanHandler(
    IWorkoutPlanRepository workoutPlanRepository,
    IExerciseRepository exerciseRepository,
    ITrainingAccessPolicy accessPolicy,
    IValidator<CreateWorkoutPlanCommand> validator)
{
    public async Task<Result<WorkoutPlanResponse>> HandleAsync(
        CreateWorkoutPlanCommand command,
        int actorUserId,
        string[] actorScopes,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<WorkoutPlanResponse>.Failure(CommonErrors.Validation(string.Join("; ", validation.Errors.Select(x => x.ErrorMessage))));

        var canCreate = await accessPolicy.CanCreateWorkoutForAsync(actorUserId, command.TargetUserId, actorScopes, cancellationToken);
        if (!canCreate)
            return Result<WorkoutPlanResponse>.Failure(TrainingErrors.CannotCreateWorkoutForTarget(actorUserId, command.TargetUserId));

        var exerciseMaps = new List<(ExerciseResponse Exercise, WorkoutExerciseDto Input)>();
        foreach (var exerciseInput in command.Exercises)
        {
            var exercise = await exerciseRepository.GetByIdAsync(exerciseInput.ExerciseId, cancellationToken);
            if (exercise is null)
                return Result<WorkoutPlanResponse>.Failure(TrainingErrors.ExerciseNotFound(exerciseInput.ExerciseId));

            exerciseMaps.Add((CreateExerciseHandler.MapResponse(exercise), exerciseInput));
        }

        var nowUtc = DateTime.UtcNow;
        var plan = new WorkoutPlanDocument
        {
            TargetUserId = command.TargetUserId,
            CreatedByUserId = actorUserId,
            TrainerUserId = actorUserId == command.TargetUserId ? null : actorUserId,
            Name = command.Name.Trim(),
            Notes = string.IsNullOrWhiteSpace(command.Notes) ? null : command.Notes.Trim(),
            DurationInWeeks = command.DurationInWeeks,
            Phase = command.Phase.Trim(),
            Difficulty = command.Difficulty,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc,
            Exercises = exerciseMaps
                .Select(x => new PlannedExerciseDocumentValueObject
                {
                    ExerciseId = x.Exercise.Id,
                    ExerciseName = x.Exercise.Name,
                    Sets = x.Input.Sets
                        .Select(s => new PlannedSetDocumentValueObject
                        {
                            Repetitions = s.Repetitions,
                            Load = s.Load,
                            LoadUnit = s.LoadUnit,
                            SetType = s.SetType,
                            Technique = s.Technique,
                            Rpe = s.Rpe,
                            RestSeconds = s.RestSeconds
                        })
                        .ToList()
                })
                .ToList()
        };

        await workoutPlanRepository.AddAsync(plan, cancellationToken);
        return Result<WorkoutPlanResponse>.Success(plan.ToResponse());
    }
}

