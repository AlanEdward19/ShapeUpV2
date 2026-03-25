namespace ShapeUp.Features.GymManagement.GymPlans.CreateGymPlan;

public record CreateGymPlanResponse(int Id, int GymId, string Name, string? Description, decimal Price, int DurationDays);

