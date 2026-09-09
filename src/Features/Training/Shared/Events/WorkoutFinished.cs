namespace ShapeUp.Features.Training.Shared.Events;

public record WorkoutFinished(string SessionId, int TargetUserId, int ExecutedByUserId, DateTime EndedAtUtc);
