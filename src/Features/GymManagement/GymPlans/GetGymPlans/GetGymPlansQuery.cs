namespace ShapeUp.Features.GymManagement.GymPlans.GetGymPlans;

public record GetGymPlansQuery(int GymId, string? Cursor, int? PageSize);