namespace ShapeUp.Features.Authorization.UserManagement.GetOrCreateUser;

public record GetUserResponse(int UserId, string Email, string? DisplayName, string[] Scopes);