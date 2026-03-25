namespace ShapeUp.Features.Authorization.UserManagement.RevokeCurrentToken;

public record RevokeCurrentTokenCommand(int UserId, string FirebaseUid);
