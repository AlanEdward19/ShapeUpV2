using ShapeUp.Features.Training.Shared.Enums;

namespace ShapeUp.Features.Training.Workouts.Shared.ValueObjects;

public record WorkoutSetValueObject(int Repetitions, decimal Load, LoadUnit LoadUnit, SetType SetType, Technique Technique, int Rpe, int RestSeconds, bool IsExtra = false);
