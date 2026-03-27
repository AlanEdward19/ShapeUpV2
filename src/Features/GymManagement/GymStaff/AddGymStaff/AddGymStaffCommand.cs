namespace ShapeUp.Features.GymManagement.GymStaff.AddGymStaff;

using Shared.Entities;

public record AddGymStaffCommand(int GymId, int UserId, GymStaffRole Role);

