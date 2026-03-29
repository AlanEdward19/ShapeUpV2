using ShapeUp.Features.Training.Workouts.Shared.Dtos;

namespace ShapeUp.Features.Training.WorkoutPlans.Shared.ViewModels;

public record WorkoutPlanResponse(
    string PlanId,
    int TargetUserId,
    int CreatedByUserId,
    int? TrainerUserId,
    string Name,
    string? Notes,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    WorkoutExerciseDto[] Exercises);

