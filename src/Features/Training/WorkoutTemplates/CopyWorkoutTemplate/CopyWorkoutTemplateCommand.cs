namespace ShapeUp.Features.Training.WorkoutTemplates.CopyWorkoutTemplate;

public record CopyWorkoutTemplateCommand(string TemplateId = "", string? Name = null);
