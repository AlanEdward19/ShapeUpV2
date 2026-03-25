namespace ShapeUp.Features.GymManagement.GymPlans.GetGymPlans;

public record GetGymPlanResponse(int Id, int GymId, string Name, string? Description, decimal Price, int DurationDays, bool IsActive);