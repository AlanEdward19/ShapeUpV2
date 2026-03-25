namespace ShapeUp.Features.Authorization.UserManagement.GetOrCreateUser;

public record GetOrCreateUserCommand(string FirebaseUid, string Email, string? DisplayName);

