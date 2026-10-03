namespace ShapeUp.Features.Credentials.ReviewCredential;

/// <summary>ExpiresAt is optional; without it the verification is valid for 12 months and must then be renewed.</summary>
public record ApproveCredentialCommand(DateTime? ExpiresAt);
