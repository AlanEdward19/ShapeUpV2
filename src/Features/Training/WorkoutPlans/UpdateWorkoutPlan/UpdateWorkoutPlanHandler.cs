using FluentValidation;
using ShapeUp.Features.Training.Exercises.CreateExercise;
using ShapeUp.Features.Training.Exercises.Shared.ViewModels;
using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Documents.ValueObjects;
using ShapeUp.Features.Training.Shared.Errors;
using ShapeUp.Features.Training.WorkoutPlans.Shared;
using ShapeUp.Features.Training.WorkoutPlans.Shared.ViewModels;
using ShapeUp.Features.Training.Workouts.Shared.Dtos;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Training.WorkoutPlans.UpdateWorkoutPlan;

public class UpdateWorkoutPlanHandler(
    IWorkoutPlanRepository workoutPlanRepository,
    IExerciseRepository exerciseRepository,
    IValidator<UpdateWorkoutPlanCommand> validator)
{
    public async Task<Result<WorkoutPlanResponse>> HandleAsync(
        UpdateWorkoutPlanCommand command,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<WorkoutPlanResponse>.Failure(CommonErrors.Validation(string.Join("; ", validation.Errors.Select(x => x.ErrorMessage))));

        var plan = await workoutPlanRepository.GetByIdAsync(command.PlanId, cancellationToken);
        if (plan is null)
            return Result<WorkoutPlanResponse>.Failure(TrainingErrors.WorkoutPlanNotFound(command.PlanId));

        if (plan.CreatedByUserId != actorUserId)
            return Result<WorkoutPlanResponse>.Failure(TrainingErrors.WorkoutPlanNotOwned(command.PlanId, actorUserId));

        var exerciseMaps = new List<(ExerciseResponse Exercise, WorkoutExerciseDto Input)>();
        foreach (var exerciseInput in command.Exercises)
        {
            var exercise = await exerciseRepository.GetByIdAsync(exerciseInput.ExerciseId, cancellationToken);
            if (exercise is null)
                return Result<WorkoutPlanResponse>.Failure(TrainingErrors.ExerciseNotFound(exerciseInput.ExerciseId));

            exerciseMaps.Add((CreateExerciseHandler.MapResponse(exercise), exerciseInput));
        }

        plan.Name = command.Name.Trim();
        plan.Notes = string.IsNullOrWhiteSpace(command.Notes) ? null : command.Notes.Trim();
        plan.DurationInWeeks = command.DurationInWeeks;
        plan.Phase = command.Phase.Trim();
        plan.Difficulty = command.Difficulty;
        plan.UpdatedAtUtc = DateTime.UtcNow;
        plan.Exercises = exerciseMaps
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
            .ToList();

        await workoutPlanRepository.UpdateAsync(plan, cancellationToken);
        return Result<WorkoutPlanResponse>.Success(plan.ToResponse());
    }
}

