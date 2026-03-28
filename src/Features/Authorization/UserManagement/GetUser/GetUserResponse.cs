namespace ShapeUp.Features.Authorization.UserManagement.GetUser;

public record GetUserResponse(int UserId, string Email, string? DisplayName, string[] Scopes);