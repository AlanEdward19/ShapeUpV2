using ShapeUp.Features.Training.Workouts.Shared.Dtos;

namespace ShapeUp.Features.Training.Workouts.FinishWorkoutExecution;

public record FinishWorkoutExecutionCommand(string SessionId = "", DateTime EndedAtUtc = default, int PerceivedExertion = 0, WorkoutExerciseDto[]? Exercises = null);
