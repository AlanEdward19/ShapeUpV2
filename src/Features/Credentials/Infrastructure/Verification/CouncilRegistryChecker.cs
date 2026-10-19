namespace ShapeUp.Features.Credentials.Infrastructure.Verification;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Abstractions;
using Shared.Entities;
using Shared.Verification;
using ShapeUp.Features.Authorization.Shared.Abstractions;

public class CouncilRegistryChecker(
    ICouncilRegistryClient client,
    IUserRepository userRepository,
    ICredentialVerificationAuditRepository auditRepository,
    IOptions<CouncilVerificationOptions> options,
    ILogger<CouncilRegistryChecker>? logger = null) : ICouncilRegistryChecker
{
    public bool IsEnabled(string authority)
    {
        var config = options.Value.For(authority);
        return config.Enabled && !string.IsNullOrWhiteSpace(config.BaseUrl);
    }

    public async Task<CouncilCheckResult> CheckAsync(ProfessionalCredential credential, CancellationToken cancellationToken)
    {
        var result = await EvaluateAsync(credential, cancellationToken);
        await AuditAsync(credential, result, cancellationToken);
        return result;
    }

    private async Task<CouncilCheckResult> EvaluateAsync(ProfessionalCredential credential, CancellationToken cancellationToken)
    {
        var lookup = await client.LookupAsync(
            credential.IssuingAuthority, credential.IssuingRegion, credential.CredentialNumber, cancellationToken);

        switch (lookup.Status)
        {
            case CouncilLookupStatus.Unavailable:
                return new CouncilCheckResult(CouncilCheckOutcome.Inconclusive, "Council lookup unavailable.");
            case CouncilLookupStatus.NotFound:
                return new CouncilCheckResult(CouncilCheckOutcome.NotFound, "Registro não encontrado no conselho.");
        }

        if (!lookup.Active)
            return new CouncilCheckResult(CouncilCheckOutcome.NotFound, "Registro encontrado, mas não está ativo no conselho.");

        if (!string.IsNullOrWhiteSpace(lookup.Region)
            && !string.Equals(lookup.Region.Trim(), credential.IssuingRegion, StringComparison.OrdinalIgnoreCase))
            return new CouncilCheckResult(CouncilCheckOutcome.Inconclusive, "Region differs from the one informed.");

        var user = await userRepository.GetByIdAsync(credential.UserId, cancellationToken);
        return PersonNameMatcher.IsStrongMatch(user?.DisplayName, lookup.Name)
            ? new CouncilCheckResult(CouncilCheckOutcome.Verified, "Registration active and name matches.")
            : new CouncilCheckResult(CouncilCheckOutcome.Inconclusive, "Name does not match the council record well enough.");
    }

    private async Task AuditAsync(ProfessionalCredential credential, CouncilCheckResult result, CancellationToken cancellationToken)
    {
        try
        {
            await auditRepository.AddAsync(new CredentialVerificationAudit
            {
                CredentialId = credential.Id,
                Source = $"council:{credential.IssuingAuthority}",
                Outcome = result.Outcome.ToString(),
                Detail = result.Reason.Length > 300 ? result.Reason[..300] : result.Reason,
                CheckedAt = DateTime.UtcNow
            }, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // The audit is a record, not a gate: losing one entry must not block or change the decision.
            logger?.LogError(ex, "Could not record the council check of credential {CredentialId}", credential.Id);
        }
    }
}
