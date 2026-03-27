namespace ShapeUp.Features.GymManagement.UserRoles.AssignUserRole;

using Shared.Entities;

public record AssignUserRoleCommand(int UserId, PlatformRoleType Role, int? PlatformTierId);

