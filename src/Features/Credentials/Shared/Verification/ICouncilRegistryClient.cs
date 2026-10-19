namespace ShapeUp.Features.Credentials.Shared.Verification;

public enum CouncilLookupStatus
{
    Found,
    NotFound,
    /// <summary>Timeout, error, circuit open or not configured: says nothing about the registration.</summary>
    Unavailable
}

public record CouncilLookupResult(CouncilLookupStatus Status, bool Active = false, string? Name = null, string? Region = null)
{
    public static CouncilLookupResult Unavailable() => new(CouncilLookupStatus.Unavailable);
}

/// <summary>Looks one registration up at a council (or at a service that fronts it).</summary>
public interface ICouncilRegistryClient
{
    Task<CouncilLookupResult> LookupAsync(string authority, string region, string number, CancellationToken cancellationToken);
}
