namespace ShapeUp.Features.GymManagement.GymStaff.GetGymStaff;

public record GetGymStaffResponse(int Id, int GymId, int UserId, string Role, DateTime HiredAt);