using ShapeUp.Features.Training.Shared.Enums;
using ShapeUp.Features.Training.Workouts.Shared.Dtos;

namespace ShapeUp.Features.Training.WorkoutTemplates.Shared.ViewModels;

public record WorkoutTemplateResponse(
    string TemplateId,
    int CreatedByUserId,
    string Name,
    string? Notes,
    int DurationInWeeks,
    string Phase,
    Difficulty Difficulty,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    WorkoutExerciseDto[] Exercises);

