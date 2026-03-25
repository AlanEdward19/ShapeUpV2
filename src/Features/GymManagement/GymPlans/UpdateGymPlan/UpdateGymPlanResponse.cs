namespace ShapeUp.Features.GymManagement.GymPlans.UpdateGymPlan;

public record UpdateGymPlanResponse(int Id, int GymId, string Name, string? Description, decimal Price, int DurationDays, bool IsActive);