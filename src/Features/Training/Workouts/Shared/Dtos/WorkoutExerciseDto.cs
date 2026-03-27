using ShapeUp.Features.Training.Workouts.Shared.ValueObjects;

namespace ShapeUp.Features.Training.Workouts.Shared.Dtos;

public record WorkoutExerciseDto(int ExerciseId, WorkoutSetValueObject[] Sets);