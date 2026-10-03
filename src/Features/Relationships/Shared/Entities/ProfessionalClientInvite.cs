namespace ShapeUp.Features.Relationships.Shared.Entities;

/// <summary>
/// Invitation from a professional to a (not yet known) client for a relationship type.
/// The client's consent is the act of accepting it with the token.
/// </summary>
public class ProfessionalClientInvite
{
    public int Id { get; set; }

    public int ProfessionalUserId { get; set; }

    public required string RelationshipType { get; set; }

    public required string TokenHash { get; set; }

    public ProfessionalClientInviteStatus Status { get; set; } = ProfessionalClientInviteStatus.Pending;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime ExpiresAtUtc { get; set; }

    public int? AcceptedByUserId { get; set; }

    public DateTime? AcceptedAtUtc { get; set; }
}
