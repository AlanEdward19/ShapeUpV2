namespace ShapeUp.Features.Training.WorkoutTemplates.AssignWorkoutTemplate;

public record AssignWorkoutTemplateCommand(string TemplateId = "", int TargetUserId = 0, string? PlanName = null);
