namespace ShapeUp.Features.Training.Workouts.Shared.ValueObjects;

public record WorkoutSetValueObject(int Repetitions, decimal Load, string LoadUnit, string SetType, int Rpe, int RestSeconds);