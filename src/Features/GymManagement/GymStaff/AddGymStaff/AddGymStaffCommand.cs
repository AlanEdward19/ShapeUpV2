namespace ShapeUp.Features.GymManagement.GymStaff.AddGymStaff;

using ShapeUp.Features.GymManagement.Shared.Entities;

public record AddGymStaffCommand(int GymId, int UserId, GymStaffRole Role);

