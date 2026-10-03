namespace ShapeUp.Features.Credentials.SubmitCredential;

/// <summary>ProfessionType is PersonalTrainer or Nutritionist; IssuingAuthority is CREF or CRN; IssuingRegion is the UF.</summary>
public record SubmitCredentialCommand(
    string ProfessionType,
    string CredentialNumber,
    string IssuingAuthority,
    string IssuingRegion,
    string Country);
