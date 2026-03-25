namespace ShapeUp.Features.Authorization.Groups.UpdateUserRole;

public record UpdateUserRoleCommand(int UserId, string NewRole);