namespace ShapeUp.Features.Credentials.Shared.Abstractions;

using Entities;

public interface IProfessionalCredentialRepository
{
    /// <summary>
    /// Returns the credential only if it is Verified and not expired as of <paramref name="nowUtc"/>.
    /// Returns null for any other status, for an expired VERIFIED credential, or if none exists.
    /// </summary>
    Task<ProfessionalCredential?> GetVerifiedAsync(
        int userId,
        string professionType,
        DateTime nowUtc,
        CancellationToken cancellationToken);

    Task<ProfessionalCredential?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<IReadOnlyList<ProfessionalCredential>> GetByUserIdAsync(int userId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ProfessionalCredential>> GetByStatusAsync(CredentialStatus status, CancellationToken cancellationToken);

    /// <summary>Keyset page (ascending Id, so oldest submission first) of the credentials in a status.</summary>
    Task<IReadOnlyList<ProfessionalCredential>> GetByStatusKeysetAsync(
        CredentialStatus status, int? afterId, int pageSize, CancellationToken cancellationToken);

    /// <summary>True when any user has a Submitted, UnderReview or Verified credential with this council registration.</summary>
    Task<bool> IsRegistrationInUseAsync(
        string issuingAuthority, string issuingRegion, string credentialNumber, CancellationToken cancellationToken);

    /// <summary>True when the user has a credential for the profession in Submitted, UnderReview or Verified.</summary>
    Task<bool> HasOpenOrVerifiedAsync(int userId, string professionType, CancellationToken cancellationToken);

    /// <summary>Verified credentials whose ExpiresAt is at or before <paramref name="nowUtc"/>.</summary>
    Task<IReadOnlyList<ProfessionalCredential>> GetExpiredVerifiedAsync(DateTime nowUtc, CancellationToken cancellationToken);

    /// <exception cref="Errors.CredentialConflictException">A unique index (one open credential per user/profession or per registration) rejected the insert.</exception>
    Task AddAsync(ProfessionalCredential credential, CancellationToken cancellationToken);

    /// <exception cref="Errors.CredentialConflictException">The credential was changed by another request since it was read.</exception>
    Task UpdateAsync(ProfessionalCredential credential, CancellationToken cancellationToken);

    /// <summary>
    /// Compensation: puts the row back to <paramref name="snapshot"/> only while it is still in <paramref name="expectedCurrent"/>
    /// (conditional UPDATE). Returns false when someone else changed it in the meantime.
    /// </summary>
    Task<bool> RevertAsync(ProfessionalCredential snapshot, CredentialStatus expectedCurrent, CancellationToken cancellationToken);
}
