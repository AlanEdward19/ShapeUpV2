using ShapeUp.Features.Training.Workouts.Shared.ValueObjects;

namespace ShapeUp.Features.Training.Workouts.Shared.Dtos;

public record ExecutedExerciseDto(int ExerciseId, string ExerciseName, ExecutedSetValueObject[] Sets);