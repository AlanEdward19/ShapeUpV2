namespace ShapeUp.Features.Training.WorkoutPlans.CopyWorkoutPlan;

public record CopyWorkoutPlanCommand(string PlanId = "", int? TargetUserId = null, string? Name = null);

