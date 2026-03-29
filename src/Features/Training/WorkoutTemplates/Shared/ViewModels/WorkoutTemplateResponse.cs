using ShapeUp.Features.Training.Workouts.Shared.Dtos;

namespace ShapeUp.Features.Training.WorkoutTemplates.Shared.ViewModels;

public record WorkoutTemplateResponse(
    string TemplateId,
    int CreatedByUserId,
    string Name,
    string? Notes,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    WorkoutExerciseDto[] Exercises);

