namespace ShapeUp.Features.GymManagement.UserRoles.AssignUserRole;

using ShapeUp.Features.GymManagement.Shared.Entities;

public record AssignUserRoleCommand(int UserId, PlatformRoleType Role, int? PlatformTierId);

