using ShapeUp.Features.Training.Shared.Enums;
using ShapeUp.Features.Training.Workouts.Shared.Dtos;

namespace ShapeUp.Features.Training.WorkoutPlans.Shared.ViewModels;

public record WorkoutPlanResponse(
    string PlanId,
    int TargetUserId,
    int CreatedByUserId,
    int? TrainerUserId,
    string Name,
    string? Notes,
    int DurationInWeeks,
    string Phase,
    Difficulty Difficulty,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    WorkoutExerciseDto[] Exercises);

