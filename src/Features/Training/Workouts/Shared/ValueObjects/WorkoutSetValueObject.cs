using ShapeUp.Features.Training.Shared.Enums;

namespace ShapeUp.Features.Training.Workouts.Shared.ValueObjects;

public record WorkoutSetValueObject(int Repetitions, decimal Load, LoadUnit LoadUnit, SetType SetType, int Rpe, int RestSeconds, bool IsExtra = false);
