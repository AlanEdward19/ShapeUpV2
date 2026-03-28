using ShapeUp.Features.Training.Exercises.Shared.ValueObjects;

namespace ShapeUp.Features.Training.Exercises.Shared.ViewModels;

public record ExerciseResponse(
    int Id,
    string Name,
    string NamePt,
    string? Description,
    string? VideoUrl,
    ExerciseMuscleValueObject[] Muscles,
    ExerciseEquipmentValueObject[] Equipments,
    string[] Steps);