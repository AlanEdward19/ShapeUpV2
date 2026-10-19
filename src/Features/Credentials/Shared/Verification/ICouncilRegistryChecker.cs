namespace ShapeUp.Features.Credentials.Shared.Verification;

using Entities;

public enum CouncilCheckOutcome
{
    /// <summary>Registration exists, is active and the name matches strongly.</summary>
    Verified,
    /// <summary>The council does not know the registration, or it is not active.</summary>
    NotFound,
    /// <summary>Unavailable council, name that does not match well enough, other region: a person decides.</summary>
    Inconclusive
}

public record CouncilCheckResult(CouncilCheckOutcome Outcome, string Reason);

/// <summary>Looks the credential up, matches the holder's name and records an audit entry (source, time, outcome only).</summary>
public interface ICouncilRegistryChecker
{
    bool IsEnabled(string authority);

    Task<CouncilCheckResult> CheckAsync(ProfessionalCredential credential, CancellationToken cancellationToken);
}
