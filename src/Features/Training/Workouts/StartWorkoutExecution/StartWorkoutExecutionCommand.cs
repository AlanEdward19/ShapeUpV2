namespace ShapeUp.Features.Training.Workouts.StartWorkoutExecution;

public record StartWorkoutExecutionCommand(string PlanId, DateTime StartedAtUtc, int? ExecutedByUserId);

