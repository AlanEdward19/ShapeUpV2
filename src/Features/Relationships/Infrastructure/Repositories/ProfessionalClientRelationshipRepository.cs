namespace ShapeUp.Features.Relationships.Infrastructure.Repositories;

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Shared.Abstractions;
using Shared.Entities;
using Shared.Data;
using ShapeUp.Shared.Results;

public class ProfessionalClientRelationshipRepository(RelationshipsDbContext context) : IProfessionalClientRelationshipRepository
{
    private const int SqlUniqueConstraintViolation = 2627;
    private const int SqlUniqueIndexViolation = 2601;

    public async Task<ProfessionalClientRelationship?> GetActiveAsync(
        int professionalUserId,
        int clientUserId,
        string relationshipType,
        CancellationToken cancellationToken)
    {
        return await context.Set<ProfessionalClientRelationship>()
            .AsNoTracking()
            .Where(x => x.ProfessionalUserId == professionalUserId
                        && x.ClientUserId == clientUserId
                        && x.RelationshipType == relationshipType
                        && x.Status == RelationshipStatus.Active)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Result<ProfessionalClientRelationship>> CreateAsync(
        ProfessionalClientRelationship relationship,
        CancellationToken cancellationToken)
    {
        await context.Set<ProfessionalClientRelationship>().AddAsync(relationship, cancellationToken);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            context.Entry(relationship).State = EntityState.Detached;
            return Result<ProfessionalClientRelationship>.Failure(CommonErrors.Conflict(
                $"An active {relationship.RelationshipType} relationship already exists between professional {relationship.ProfessionalUserId} and client {relationship.ClientUserId}."));
        }

        return Result<ProfessionalClientRelationship>.Success(relationship);
    }

    public async Task<IReadOnlyList<ProfessionalClientRelationship>> ListActiveByProfessionalAsync(
        int professionalUserId,
        string relationshipType,
        CancellationToken cancellationToken)
    {
        return await context.Set<ProfessionalClientRelationship>()
            .AsNoTracking()
            .Where(x => x.ProfessionalUserId == professionalUserId
                        && x.RelationshipType == relationshipType
                        && x.Status == RelationshipStatus.Active)
            .OrderByDescending(x => x.StartedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProfessionalClientRelationship>> ListActiveByProfessionalKeysetAsync(
        int professionalUserId,
        string relationshipType,
        int? afterId,
        int pageSize,
        CancellationToken cancellationToken)
    {
        return await context.Set<ProfessionalClientRelationship>()
            .AsNoTracking()
            .Where(x => x.ProfessionalUserId == professionalUserId
                        && x.RelationshipType == relationshipType
                        && x.Status == RelationshipStatus.Active
                        && (afterId == null || x.Id > afterId))
            .OrderBy(x => x.Id)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProfessionalClientRelationship>> ListActiveByClientAsync(
        int clientUserId,
        string relationshipType,
        CancellationToken cancellationToken)
    {
        return await context.Set<ProfessionalClientRelationship>()
            .AsNoTracking()
            .Where(x => x.ClientUserId == clientUserId
                        && x.RelationshipType == relationshipType
                        && x.Status == RelationshipStatus.Active)
            .OrderByDescending(x => x.StartedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> EndActiveAsync(
        int professionalUserId,
        int clientUserId,
        string relationshipType,
        DateTime endedAtUtc,
        CancellationToken cancellationToken)
    {
        var relationship = await context.Set<ProfessionalClientRelationship>()
            .FirstOrDefaultAsync(x => x.ProfessionalUserId == professionalUserId
                                      && x.ClientUserId == clientUserId
                                      && x.RelationshipType == relationshipType
                                      && x.Status == RelationshipStatus.Active,
                cancellationToken);

        if (relationship is null)
            return false;

        relationship.Status = RelationshipStatus.Ended;
        relationship.EndedAt = endedAtUtc;
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex) =>
        ex.InnerException is SqlException { Number: SqlUniqueConstraintViolation or SqlUniqueIndexViolation };
}
