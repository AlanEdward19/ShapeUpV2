namespace ShapeUp.Features.Credentials.ExpireCredentials;

using Shared.Abstractions;
using Shared.Entities;
using Shared.Lifecycle;
using Microsoft.Extensions.Logging;

/// <summary>Expires Verified credentials past ExpiresAt and withdraws the roles they granted.</summary>
public class ExpireCredentialsHandler(
    IProfessionalCredentialRepository repository,
    ICredentialLifecycle lifecycle,
    ILogger<ExpireCredentialsHandler>? logger = null)
{
    /// <returns>How many credentials were expired.</returns>
    public async Task<int> HandleAsync(DateTime nowUtc, CancellationToken cancellationToken)
    {
        var due = await repository.GetExpiredVerifiedAsync(nowUtc, cancellationToken);

        var expired = 0;
        foreach (var credential in due)
        {
            try
            {
                var result = await lifecycle.EndAsync(credential, CredentialStatus.Expired, nowUtc, cancellationToken);
                if (result.IsSuccess)
                    expired++;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                // One bad credential must not stop the rest of the batch; it is picked up again on the next run.
                logger?.LogError(ex, "Failed to expire credential {CredentialId}", credential.Id);
            }
        }

        return expired;
    }
}
