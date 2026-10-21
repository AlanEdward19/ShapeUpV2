namespace ShapeUp.Features.Authorization.UserManagement.SetUserActive;

public record SetUserActiveCommand(int UserId, bool IsActive, int ActorUserId);

public record SetUserActiveResponse(int UserId, string Email, string? DisplayName, bool IsActive);
