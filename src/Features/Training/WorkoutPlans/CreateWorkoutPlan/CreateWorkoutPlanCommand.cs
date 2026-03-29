using ShapeUp.Features.Training.Workouts.Shared.Dtos;

namespace ShapeUp.Features.Training.WorkoutPlans.CreateWorkoutPlan;

public record CreateWorkoutPlanCommand(
    int TargetUserId,
    string Name,
    string? Notes,
    WorkoutExerciseDto[] Exercises);

