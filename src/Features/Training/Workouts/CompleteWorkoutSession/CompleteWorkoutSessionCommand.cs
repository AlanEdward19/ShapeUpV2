namespace ShapeUp.Features.Training.Workouts.CompleteWorkoutSession;

public record CompleteWorkoutSessionCommand(string SessionId, DateTime EndedAtUtc, int PerceivedExertion);