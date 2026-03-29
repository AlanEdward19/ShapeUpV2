using ShapeUp.Features.Training.Workouts.Shared.Dtos;

namespace ShapeUp.Features.Training.Workouts.UpdateWorkoutExecutionState;

public record UpdateWorkoutExecutionStateCommand(string SessionId = "", DateTime SavedAtUtc = default, WorkoutExerciseDto[] Exercises = null!);
