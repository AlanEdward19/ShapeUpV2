using FluentValidation;
using ShapeUp.Features.Training.Exercises.CreateExercise;
using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Documents.ValueObjects;
using ShapeUp.Features.Training.Shared.Enums;
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

        var plan = await workoutPlanRepository.GetByIdAsync(command.GetPlanId(), cancellationToken);
        if (plan is null)
            return Result<WorkoutPlanResponse>.Failure(TrainingErrors.WorkoutPlanNotFound(command.GetPlanId()));

        if (plan.CreatedByUserId != actorUserId)
            return Result<WorkoutPlanResponse>.Failure(TrainingErrors.WorkoutPlanNotOwned(command.GetPlanId(), actorUserId));

        var blocks = new List<BlockDocumentValueObject>();
        foreach (var blockInput in command.Blocks)
        {
            var exercises = new List<BlockExerciseDocumentValueObject>();
            foreach (var exerciseInput in blockInput.Exercises)
            {
                var exercise = await exerciseRepository.GetByIdAsync(exerciseInput.ExerciseId, cancellationToken);
                if (exercise is null)
                    return Result<WorkoutPlanResponse>.Failure(TrainingErrors.ExerciseNotFound(exerciseInput.ExerciseId));

                var mapped = CreateExerciseHandler.MapResponse(exercise);
                if (mapped.ExerciseType == ExerciseType.TimeBased)
                {
                    foreach (var setInput in exerciseInput.Sets)
                    {
                        if (setInput.DurationSeconds is null || setInput.DurationSeconds <= 0)
                            return Result<WorkoutPlanResponse>.Failure(TrainingErrors.DurationRequiredForExercise(mapped.Id));

                        if (setInput.Technique != Technique.Straight)
                            return Result<WorkoutPlanResponse>.Failure(TrainingErrors.TechniqueNotAllowedForTimeBasedExercise(mapped.Id));
                    }
                }

                exercises.Add(new BlockExerciseDocumentValueObject
                {
                    ExerciseId = mapped.Id,
                    ExerciseName = mapped.Name,
                    StrengthGainPercentage = exerciseInput.StrengthGainPercentage,
                    RequireRpe = exerciseInput.RequireRpe,
                    Sets = exerciseInput.Sets
                        .Select(s => new PlannedSetDocumentValueObject
                        {
                            Repetitions = s.Repetitions,
                            Load = s.Load,
                            LoadUnit = s.LoadUnit,
                            SetType = s.SetType,
                            Technique = s.Technique,
                            Intensity = s.Intensity is null ? null : new IntensityDocumentValueObject { Type = s.Intensity.Type, Value = s.Intensity.Value },
                            RestSeconds = s.RestSeconds,
                            DurationSeconds = s.DurationSeconds,
                            DistanceMeters = s.DistanceMeters
                        })
                        .ToList()
                });
            }

            blocks.Add(new BlockDocumentValueObject
            {
                Type = blockInput.Type,
                Exercises = exercises,
                TimeCapSeconds = blockInput.TimeCapSeconds,
                IntervalSeconds = blockInput.IntervalSeconds,
                TotalRounds = blockInput.TotalRounds,
                RestAfterSeconds = blockInput.RestAfterSeconds
            });
        }

        plan.Name = command.Name.Trim();
        plan.Notes = string.IsNullOrWhiteSpace(command.Notes) ? null : command.Notes.Trim();
        plan.DurationInWeeks = command.DurationInWeeks;
        plan.Phase = command.Phase.Trim();
        plan.Difficulty = command.Difficulty;
        plan.UpdatedAtUtc = DateTime.UtcNow;
        plan.AssignedWeekdays = (command.AssignedWeekdays ?? []).Distinct().ToList();
        plan.Blocks = blocks;

        await workoutPlanRepository.UpdateAsync(plan, cancellationToken);
        return Result<WorkoutPlanResponse>.Success(plan.ToResponse());
    }
}

