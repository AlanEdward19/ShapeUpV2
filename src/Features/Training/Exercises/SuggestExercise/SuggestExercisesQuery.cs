namespace ShapeUp.Features.Training.Exercises.SuggestExercise;

using ShapeUp.Features.Training.Shared.Enums;

public record SuggestExercisesQuery(string Name, EMuscleGroup[] MuscleGroups, int[] EquipmentIds, int? Limit);
