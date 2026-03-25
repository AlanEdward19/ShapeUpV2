namespace ShapeUp.Features.Authorization.UserManagement.RevokeCurrentToken;

public record RevokeCurrentTokenResponse(int UserId, DateTime RevokedAtUtc);
