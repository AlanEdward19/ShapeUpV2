using ShapeUp.Features.Training.Shared.Enums;
using ShapeUp.Features.Training.Workouts.Shared.Dtos;

namespace ShapeUp.Features.Training.WorkoutTemplates.UpdateWorkoutTemplate;

public record UpdateWorkoutTemplateCommand(
    string TemplateId,
    string Name,
    string? Notes,
    int DurationInWeeks,
    string Phase,
    Difficulty Difficulty,
    WorkoutExerciseDto[] Exercises);

