using ShapeUp.Features.Training.Workouts.Shared.Dtos;

namespace ShapeUp.Features.Training.WorkoutTemplates.CreateWorkoutTemplate;

public record CreateWorkoutTemplateCommand(string Name, string? Notes, WorkoutExerciseDto[] Exercises);

