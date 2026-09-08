using ShapeUp.Features.Training.Shared.Enums;
using ShapeUp.Features.Training.Workouts.Shared.Dtos;

namespace ShapeUp.Features.Training.Workouts.Shared.ValueObjects;

public record WorkoutSetValueObject(int? Repetitions, decimal Load, LoadUnit LoadUnit, SetType SetType, Technique Technique, IntensityDto? Intensity, int? RestSeconds, bool IsExtra = false);
