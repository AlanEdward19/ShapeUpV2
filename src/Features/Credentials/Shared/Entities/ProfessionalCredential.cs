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

    public DateTime? SubmittedAt { get; set; }

    public DateTime? ReviewedAt { get; set; }

    /// <summary>Admin who approved/rejected it; null when the verifier decided automatically.</summary>
    public int? ReviewedByUserId { get; set; }

    public string? RejectionReason { get; set; }

    /// <summary>When a Verified credential was suspended, revoked or expired.</summary>
    public DateTime? EndedAt { get; set; }

    /// <summary>Admin who suspended/revoked it; null for expiry.</summary>
    public int? EndedByUserId { get; set; }

    public string? EndReason { get; set; }

    /// <summary>Concurrency token: two concurrent decisions on the same credential cannot both win.</summary>
    public byte[]? RowVersion { get; set; }

    /// <summary>Copy of the current state, used to put the row back when a follow-up step fails.</summary>
    public ProfessionalCredential Snapshot() => (ProfessionalCredential)MemberwiseClone();
}
