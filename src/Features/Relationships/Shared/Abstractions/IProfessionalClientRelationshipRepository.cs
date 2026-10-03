namespace ShapeUp.Features.Relationships.Shared.Abstractions;

using Entities;
using ShapeUp.Shared.Results;

public interface IProfessionalClientRelationshipRepository
{
    /// <summary>
    /// Returns the Active relationship for the given pair and type, or null if none exists
    /// (including when it exists but has been Ended).
    /// </summary>
    Task<ProfessionalClientRelationship?> GetActiveAsync(
        int professionalUserId,
        int clientUserId,
        string relationshipType,
        CancellationToken cancellationToken);

    /// <summary>
    /// Creates an Active relationship. Fails with a Conflict error if an Active relationship
    /// already exists for the same (professional, client, type) — enforced by a unique
    /// filtered database index, not by an application-level check-then-insert.
    /// </summary>
    Task<Result<ProfessionalClientRelationship>> CreateAsync(
        ProfessionalClientRelationship relationship,
        CancellationToken cancellationToken);

    /// <summary>Active relationships of the professional for the type, newest first.</summary>
    Task<IReadOnlyList<ProfessionalClientRelationship>> ListActiveByProfessionalAsync(
        int professionalUserId,
        string relationshipType,
        CancellationToken cancellationToken);

    /// <summary>Active relationships of the client for the type, newest first.</summary>
    Task<IReadOnlyList<ProfessionalClientRelationship>> ListActiveByClientAsync(
        int clientUserId,
        string relationshipType,
        CancellationToken cancellationToken);

    /// <summary>Ends the Active relationship for the pair and type. Returns false if none was active.</summary>
    Task<bool> EndActiveAsync(
        int professionalUserId,
        int clientUserId,
        string relationshipType,
        DateTime endedAtUtc,
        CancellationToken cancellationToken);
}
