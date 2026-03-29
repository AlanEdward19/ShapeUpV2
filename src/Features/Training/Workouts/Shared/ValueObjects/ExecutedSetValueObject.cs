namespace ShapeUp.Features.Training.Workouts.Shared.ValueObjects;

public record ExecutedSetValueObject(int Repetitions, decimal Load, string LoadUnit, string SetType, int Rpe, int RestSeconds, decimal Volume, bool IsExtra);
