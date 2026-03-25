namespace ShapeUp.Features.GymManagement.GymPlans.CreateGymPlan;

public record CreateGymPlanCommand(int GymId, string Name, string? Description, decimal Price, int DurationDays);

