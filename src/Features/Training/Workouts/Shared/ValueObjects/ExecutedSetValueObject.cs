using ShapeUp.Features.Training.Shared.Enums;

namespace ShapeUp.Features.Training.Workouts.Shared.ValueObjects;

public record ExecutedSetValueObject(int Repetitions, decimal Load, LoadUnit LoadUnit, SetType SetType, int Rpe, int RestSeconds, decimal Volume, bool IsExtra);
