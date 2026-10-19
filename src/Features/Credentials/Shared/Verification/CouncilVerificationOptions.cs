namespace ShapeUp.Features.Credentials.Shared.Verification;

/// <summary>
/// Configuration under <c>Credentials:Verification</c>. Each council is off until an official API,
/// agreement or third-party service is configured (neither CFN nor CONFEF publishes one).
/// </summary>
public sealed class CouncilVerificationOptions
{
    public const string SectionName = "Credentials:Verification";

    public CouncilEndpointOptions Cref { get; set; } = new();

    public CouncilEndpointOptions Crn { get; set; } = new();

    public CouncilEndpointOptions For(string authority) => authority switch
    {
        Entities.CredentialAuthorities.Cref => Cref,
        Entities.CredentialAuthorities.Crn => Crn,
        _ => new CouncilEndpointOptions()
    };
}

public sealed class CouncilEndpointOptions
{
    /// <summary>Feature flag per council. Off: every submission goes to manual review.</summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Lookup service. Called as <c>GET {BaseUrl}?number=..&amp;region=..</c> and answers
    /// <c>{ "found": bool, "active": bool, "name": string, "region": string }</c> (404 also means not found).
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>Sent as <c>Authorization: Bearer</c> when set.</summary>
    public string ApiKey { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 8;

    /// <summary>Retries after a timeout or 5xx before giving up (the submission then falls back to manual review).</summary>
    public int Retries { get; set; } = 2;

    /// <summary>Consecutive failures that open the circuit; while open the council is not called.</summary>
    public int BreakerThreshold { get; set; } = 5;

    public int BreakerMinutes { get; set; } = 5;

    /// <summary>True: a registration the council does not know is rejected on the spot. False: it goes to a reviewer.</summary>
    public bool RejectWhenNotFound { get; set; } = true;

    /// <summary>Verified registrations are checked again after this many days.</summary>
    public int ReverifyAfterDays { get; set; } = 30;
}
