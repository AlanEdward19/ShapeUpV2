namespace ShapeUp.Features.Training.WorkoutPlans.GetWorkoutPlansByUser;

public record GetWorkoutPlansByUserQuery(int TargetUserId, string? Cursor, int? PageSize);

