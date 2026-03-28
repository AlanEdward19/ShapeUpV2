namespace ShapeUp.Features.Training.Workouts.GetWorkoutSessionsByUser;

public record GetWorkoutSessionsByUserQuery(int TargetUserId, string? Cursor, int? PageSize);