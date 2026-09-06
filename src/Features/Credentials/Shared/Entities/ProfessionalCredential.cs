namespace ShapeUp.Features.Credentials.Shared.Entities;

public class ProfessionalCredential
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public required string ProfessionType { get; set; }

    public required string CredentialNumber { get; set; }

    public required string IssuingAuthority { get; set; }

    public required string IssuingRegion { get; set; }

    public required string Country { get; set; }

    public CredentialStatus Status { get; set; } = CredentialStatus.Draft;

    public DateTime? VerifiedAt { get; set; }

    public DateTime? ExpiresAt { get; set; }
}
