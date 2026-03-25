namespace ShapeUp.Features.Authorization.UserManagement.GetOrCreateUser;

public record GetOrCreateUserResponse(int UserId, string Email, string? DisplayName, string[] Scopes);