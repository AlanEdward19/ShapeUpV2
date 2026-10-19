namespace ShapeUp.Features.Credentials.Shared.Entities;

public enum CredentialStatus
{
    Draft,
    Submitted,
    UnderReview,
    Verified,
    Rejected,
    Expired,
    Suspended,
    Revoked,
    /// <summary>The reviewer asked for a correction; the professional edits and resubmits the same credential.</summary>
    NeedsChanges
}
