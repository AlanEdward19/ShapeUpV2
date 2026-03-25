namespace ShapeUp.Features.GymManagement.GymPlans.UpdateGymPlan;

public record UpdateGymPlanCommand(int PlanId, int GymId, string Name, string? Description, decimal Price, int DurationDays, bool IsActive);

