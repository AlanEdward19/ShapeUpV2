namespace ShapeUp.Features.GymManagement.GymStaff.GetGymStaff;

public record GetGymStaffQuery(int GymId, string? Cursor, int? PageSize);