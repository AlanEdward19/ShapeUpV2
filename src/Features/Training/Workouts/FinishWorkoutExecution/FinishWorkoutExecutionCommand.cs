using ShapeUp.Features.Training.Workouts.Shared.Dtos;

namespace ShapeUp.Features.Training.Workouts.FinishWorkoutExecution;

public record FinishWorkoutExecutionCommand(string SessionId = "", DateTime? EndedAtUtc = null, int PerceivedExertion = 0, WorkoutExerciseDto[]? Exercises = null);
