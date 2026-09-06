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
}
