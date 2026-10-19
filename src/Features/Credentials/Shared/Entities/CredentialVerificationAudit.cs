namespace ShapeUp.Features.Credentials.Shared.Entities;

/// <summary>One council lookup: which source, when and what came out. The council's page or payload is not kept.</summary>
public class CredentialVerificationAudit
{
    public int Id { get; set; }

    public int CredentialId { get; set; }

    public required string Source { get; set; }

    public required string Outcome { get; set; }

    public string? Detail { get; set; }

    public DateTime CheckedAt { get; set; }
}
