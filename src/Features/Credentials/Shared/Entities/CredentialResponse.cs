namespace ShapeUp.Features.Credentials.Shared.Entities;

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
    string? RejectionReason)
{
    public static CredentialResponse From(ProfessionalCredential c) => new(
        c.Id, c.UserId, c.ProfessionType, c.CredentialNumber, c.IssuingAuthority, c.IssuingRegion,
        c.Country, c.Status.ToString(), c.SubmittedAt, c.ReviewedAt, c.VerifiedAt, c.ExpiresAt, c.RejectionReason);
}
