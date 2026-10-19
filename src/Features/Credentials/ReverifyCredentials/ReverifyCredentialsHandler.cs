namespace ShapeUp.Features.Credentials.ReverifyCredentials;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Abstractions;
using Shared.Entities;
using Shared.Lifecycle;
using Shared.Verification;

/// <summary>
/// Rechecks Verified registrations against the council (suspension or cassation happens after approval).
/// A registration the council no longer confirms is suspended and the role it granted is withdrawn; an
/// inconclusive answer (outage, name mismatch) changes nothing and is retried on the next run.
/// </summary>
public class ReverifyCredentialsHandler(
    IProfessionalCredentialRepository repository,
    ICouncilRegistryChecker checker,
    ICredentialLifecycle lifecycle,
    IOptions<CouncilVerificationOptions> options,
    ILogger<ReverifyCredentialsHandler>? logger = null)
{
    public const int BatchSize = 50;

    public const string SuspensionReason = "Registro não confirmado na reverificação periódica do conselho.";

    /// <returns>How many credentials were suspended.</returns>
    public async Task<int> HandleAsync(DateTime nowUtc, CancellationToken cancellationToken)
    {
        var suspended = 0;
        foreach (var authority in new[] { CredentialAuthorities.Cref, CredentialAuthorities.Crn })
        {
            if (!checker.IsEnabled(authority))
                continue;

            var cutoff = nowUtc.AddDays(-Math.Max(1, options.Value.For(authority).ReverifyAfterDays));
            var due = await repository.GetVerifiedDueForCouncilCheckAsync(authority, cutoff, nowUtc, BatchSize, cancellationToken);
            foreach (var credential in due)
            {
                try
                {
                    var check = await checker.CheckAsync(credential, cancellationToken);
                    if (check.Outcome == CouncilCheckOutcome.NotFound)
                    {
                        var ended = await lifecycle.EndAsync(
                            credential, CredentialStatus.Suspended, nowUtc, cancellationToken, SuspensionReason);
                        if (ended.IsSuccess)
                            suspended++;
                        continue;
                    }

                    // Unavailable council: leave the date alone so the next run tries again.
                    if (check.Outcome == CouncilCheckOutcome.Verified)
                    {
                        credential.LastCouncilCheckAt = nowUtc;
                        await repository.UpdateAsync(credential, cancellationToken);
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "Failed to reverify credential {CredentialId}", credential.Id);
                }
            }
        }

        return suspended;
    }
}
