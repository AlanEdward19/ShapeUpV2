namespace ShapeUp.Features.Credentials.ReviewCredential;

/// <summary>What the professional must correct; shown in the app and sent by e-mail.</summary>
public record RequestChangesCommand(string Message);
