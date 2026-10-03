namespace ShapeUp.Features.Credentials.ExpireCredentials;

using Shared.Abstractions;
using Shared.Entities;
using Shared.Lifecycle;

/// <summary>Expires Verified credentials past ExpiresAt and withdraws the roles they granted.</summary>
public class ExpireCredentialsHandler(
    IProfessionalCredentialRepository repository,
    ICredentialLifecycle lifecycle)
{
    /// <returns>How many credentials were expired.</returns>
    public async Task<int> HandleAsync(DateTime nowUtc, CancellationToken cancellationToken)
    {
        var due = await repository.GetExpiredVerifiedAsync(nowUtc, cancellationToken);

        var expired = 0;
        foreach (var credential in due)
        {
            var result = await lifecycle.EndAsync(credential, CredentialStatus.Expired, nowUtc, cancellationToken);
            if (result.IsSuccess)
                expired++;
        }

        return expired;
    }
}
