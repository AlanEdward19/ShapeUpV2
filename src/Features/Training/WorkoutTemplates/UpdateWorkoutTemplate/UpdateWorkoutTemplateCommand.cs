using ShapeUp.Features.Training.Shared.Enums;
using ShapeUp.Features.Training.Workouts.Shared.Dtos;

namespace ShapeUp.Features.Training.WorkoutTemplates.UpdateWorkoutTemplate;

public record UpdateWorkoutTemplateCommand(
    string Name,
    string? Notes,
    int DurationInWeeks,
    string Phase,
    Difficulty Difficulty,
    BlockDto[] Blocks)
{
    private string TemplateId { get; set; } = null!;
    
    public void SetTemplateId(string templateId)
    {
        TemplateId = templateId;
    }
    
    public string GetTemplateId()
    {
        return TemplateId;
    }
}

