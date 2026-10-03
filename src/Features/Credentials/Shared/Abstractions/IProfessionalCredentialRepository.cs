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

    /// <summary>True when the user has a credential for the profession in Submitted, UnderReview or Verified.</summary>
    Task<bool> HasOpenOrVerifiedAsync(int userId, string professionType, CancellationToken cancellationToken);

    /// <summary>Verified credentials whose ExpiresAt is at or before <paramref name="nowUtc"/>.</summary>
    Task<IReadOnlyList<ProfessionalCredential>> GetExpiredVerifiedAsync(DateTime nowUtc, CancellationToken cancellationToken);

    Task AddAsync(ProfessionalCredential credential, CancellationToken cancellationToken);

    Task UpdateAsync(ProfessionalCredential credential, CancellationToken cancellationToken);
}
