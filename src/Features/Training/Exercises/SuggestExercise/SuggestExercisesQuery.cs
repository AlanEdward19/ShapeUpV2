namespace ShapeUp.Features.Training.Exercises.SuggestExercise;

public record SuggestExercisesQuery(string Name, int[] MuscleIds, int[] EquipmentIds, int? Limit);