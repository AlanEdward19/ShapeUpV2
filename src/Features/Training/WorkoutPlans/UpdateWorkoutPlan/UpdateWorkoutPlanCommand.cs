using ShapeUp.Features.Training.Shared.Enums;
using ShapeUp.Features.Training.Workouts.Shared.Dtos;

namespace ShapeUp.Features.Training.WorkoutPlans.UpdateWorkoutPlan;

public record UpdateWorkoutPlanCommand(
    string Name,
    string? Notes,
    int DurationInWeeks,
    string Phase,
    Difficulty Difficulty,
    WorkoutExerciseDto[] Exercises)
{
    private string PlanId { get; set; } = null!;
    
    public void SetPlanId(string planId)
    {
        PlanId = planId;
    }
    
    public string GetPlanId()
    {
        return PlanId;
    }
}

