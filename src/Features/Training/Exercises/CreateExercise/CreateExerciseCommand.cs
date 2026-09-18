using ShapeUp.Features.Training.Exercises.Shared.Dtos;
using ShapeUp.Features.Training.Shared.Enums;

namespace ShapeUp.Features.Training.Exercises.CreateExercise;

public record CreateExerciseCommand(
    string Name,
    string NamePt,
    string? Description,
    string? VideoUrl,
    ExerciseMuscleDto[] Muscles,
    int[] EquipmentIds,
    ExerciseStepDto[]? Steps,
    ExerciseType ExerciseType = ExerciseType.WeightBased);