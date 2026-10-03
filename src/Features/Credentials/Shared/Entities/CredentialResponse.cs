namespace ShapeUp.Features.Credentials.Shared.Entities;

/// <summary>RequesterName/RequesterEmail are filled only on the admin review endpoints, so the reviewer sees who is asking.</summary>
public record CredentialResponse(
    int Id,
    int UserId,
    string ProfessionType,
    string CredentialNumber,
    string IssuingAuthority,
    string IssuingRegion,
    string Country,
    string Status,
    DateTime? SubmittedAt,
    DateTime? ReviewedAt,
    DateTime? VerifiedAt,
    DateTime? ExpiresAt,
    string? RejectionReason,
    DateTime? EndedAt = null,
    string? EndReason = null,
    string? RequesterName = null,
    string? RequesterEmail = null)
{
    public static CredentialResponse From(ProfessionalCredential c, string? requesterName = null, string? requesterEmail = null) => new(
        c.Id, c.UserId, c.ProfessionType, c.CredentialNumber, c.IssuingAuthority, c.IssuingRegion,
        c.Country, c.Status.ToString(), c.SubmittedAt, c.ReviewedAt, c.VerifiedAt, c.ExpiresAt, c.RejectionReason,
        c.EndedAt, c.EndReason, requesterName, requesterEmail);
}
