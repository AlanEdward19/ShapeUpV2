using ShapeUp.Features.Training.Workouts.Shared.ValueObjects;

namespace ShapeUp.Features.Training.Workouts.MarkWorkoutSet;

public record MarkWorkoutSetCommand(string SessionId = "", string OperationId = "", int ExerciseId = 0, WorkoutSetValueObject Set = null!);
