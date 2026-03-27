namespace ShapeUp.Features.Training.Exercises.Shared.ValueObjects;

using ShapeUp.Features.Training.Shared.Enums;

public record ExerciseMuscleValueObject(MuscleGroup MuscleGroup, string MuscleName, string MuscleNamePt, decimal ActivationPercent);
