namespace ShapeUp.Features.Credentials.ResubmitCredential;

/// <summary>The corrected registration data; profession, council and country stay as submitted.</summary>
public record ResubmitCredentialCommand(string CredentialNumber, string IssuingRegion);
