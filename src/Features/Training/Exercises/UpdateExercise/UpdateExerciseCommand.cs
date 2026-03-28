using ShapeUp.Features.Training.Exercises.Shared.Dtos;

namespace ShapeUp.Features.Training.Exercises.UpdateExercise;

public record UpdateExerciseCommand(
    int ExerciseId,
    string Name,
    string NamePt,
    string? Description,
    string? VideoUrl,
    ExerciseMuscleDto[] Muscles,
    int[] EquipmentIds,
    ExerciseStepDto[]? Steps);