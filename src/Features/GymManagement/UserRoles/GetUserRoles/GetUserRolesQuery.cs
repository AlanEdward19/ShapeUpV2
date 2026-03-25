namespace ShapeUp.Features.GymManagement.UserRoles.GetUserRoles;

public record GetUserRolesQuery(int UserId);

public record GetUserRoleResponse(int Id, int UserId, string Role, int? PlatformTierId, string? PlatformTierName);

