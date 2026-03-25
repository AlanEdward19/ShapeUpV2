namespace ShapeUp.Features.GymManagement.TrainerPlans.CreateTrainerPlan;

public record CreateTrainerPlanCommand(string Name, string? Description, decimal Price, int DurationDays);

