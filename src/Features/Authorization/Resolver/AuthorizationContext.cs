namespace ShapeUp.Features.Authorization.Resolver;

/// <summary>
/// Context supplied by the caller for a single capability check. Only the fields relevant to
/// the capability being checked need to be populated -- the resolver only consults the source
/// each populated field maps to (see CapabilityResolver).
/// </summary>
public sealed record AuthorizationContext(
    int? GymId = null,
    int? TargetUserId = null,
    string? RelationshipType = null,
    string? RequiredProfessionType = null,
    string? RequiredEntitlementCapability = null);
