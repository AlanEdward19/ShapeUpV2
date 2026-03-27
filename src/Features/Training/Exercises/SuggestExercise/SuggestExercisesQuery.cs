namespace ShapeUp.Features.Training.Exercises.SuggestExercise;

using ShapeUp.Features.Training.Shared.Enums;

public record SuggestExercisesQuery(string Name, MuscleGroup[] MuscleGroups, int[] EquipmentIds, int? Limit);
