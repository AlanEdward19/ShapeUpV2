namespace ShapeUp.Features.Credentials.Infrastructure.Repositories;

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Shared.Abstractions;
using Shared.Errors;
using Shared.Entities;
using Shared.Data;

public class ProfessionalCredentialRepository(CredentialsDbContext context) : IProfessionalCredentialRepository
{
    public async Task<ProfessionalCredential?> GetVerifiedAsync(
        int userId,
        string professionType,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        return await context.Set<ProfessionalCredential>()
            .AsNoTracking()
            .Where(x => x.UserId == userId
                        && x.ProfessionType == professionType
                        && x.Status == CredentialStatus.Verified
                        && (x.ExpiresAt == null || x.ExpiresAt > nowUtc))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ProfessionalCredential?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        await context.ProfessionalCredentials
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ProfessionalCredential>> GetByUserIdAsync(int userId, CancellationToken cancellationToken) =>
        await context.ProfessionalCredentials
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.Id)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ProfessionalCredential>> GetByStatusAsync(CredentialStatus status, CancellationToken cancellationToken) =>
        await context.ProfessionalCredentials
            .AsNoTracking()
            .Where(x => x.Status == status)
            .OrderBy(x => x.SubmittedAt)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ProfessionalCredential>> GetByStatusKeysetAsync(
        CredentialStatus status, int? afterId, int pageSize, CancellationToken cancellationToken) =>
        await context.ProfessionalCredentials
            .AsNoTracking()
            .Where(x => x.Status == status && (afterId == null || x.Id > afterId))
            .OrderBy(x => x.Id)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

    public async Task<bool> IsRegistrationInUseAsync(
        string issuingAuthority, string issuingRegion, string credentialNumber, CancellationToken cancellationToken) =>
        await context.ProfessionalCredentials
            .AnyAsync(x => x.IssuingAuthority == issuingAuthority
                           && x.IssuingRegion == issuingRegion
                           && x.CredentialNumber == credentialNumber
                           && (x.Status == CredentialStatus.Submitted
                               || x.Status == CredentialStatus.UnderReview
                               || x.Status == CredentialStatus.Verified), cancellationToken);

    public async Task<bool> HasOpenOrVerifiedAsync(int userId, string professionType, CancellationToken cancellationToken) =>
        await context.ProfessionalCredentials
            .AnyAsync(x => x.UserId == userId
                           && x.ProfessionType == professionType
                           && (x.Status == CredentialStatus.Submitted
                               || x.Status == CredentialStatus.UnderReview
                               || x.Status == CredentialStatus.Verified), cancellationToken);

    public async Task<IReadOnlyList<ProfessionalCredential>> GetExpiredVerifiedAsync(DateTime nowUtc, CancellationToken cancellationToken) =>
        await context.ProfessionalCredentials
            .AsNoTracking()
            .Where(x => x.Status == CredentialStatus.Verified && x.ExpiresAt != null && x.ExpiresAt <= nowUtc)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(ProfessionalCredential credential, CancellationToken cancellationToken)
    {
        await context.ProfessionalCredentials.AddAsync(credential, cancellationToken);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ToConflict(ex) is { } conflict)
        {
            context.Entry(credential).State = EntityState.Detached;
            throw conflict;
        }
    }

    public async Task UpdateAsync(ProfessionalCredential credential, CancellationToken cancellationToken)
    {
        context.ProfessionalCredentials.Update(credential);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            context.Entry(credential).State = EntityState.Detached;
            throw new CredentialConflictException(CredentialConflictKind.ConcurrentChange, ex);
        }
        catch (DbUpdateException ex) when (ToConflict(ex) is { } conflict)
        {
            context.Entry(credential).State = EntityState.Detached;
            throw conflict;
        }
    }

    public async Task<bool> RevertAsync(ProfessionalCredential snapshot, CredentialStatus expectedCurrent, CancellationToken cancellationToken)
    {
        var rows = await context.ProfessionalCredentials
            .Where(x => x.Id == snapshot.Id && x.Status == expectedCurrent)
            .ExecuteUpdateAsync(set => set
                .SetProperty(x => x.Status, snapshot.Status)
                .SetProperty(x => x.VerifiedAt, snapshot.VerifiedAt)
                .SetProperty(x => x.ExpiresAt, snapshot.ExpiresAt)
                .SetProperty(x => x.ReviewedAt, snapshot.ReviewedAt)
                .SetProperty(x => x.ReviewedByUserId, snapshot.ReviewedByUserId)
                .SetProperty(x => x.RejectionReason, snapshot.RejectionReason)
                .SetProperty(x => x.EndedAt, snapshot.EndedAt)
                .SetProperty(x => x.EndedByUserId, snapshot.EndedByUserId)
                .SetProperty(x => x.EndReason, snapshot.EndReason), cancellationToken);
        return rows > 0;
    }

    private static CredentialConflictException? ToConflict(DbUpdateException ex)
    {
        // 2601 = duplicate key in a unique index, 2627 = unique constraint.
        if (ex.InnerException is not SqlException { Number: 2601 or 2627 } sql)
            return null;

        if (sql.Message.Contains(CredentialsDbContext.OpenPerRegistrationIndex, StringComparison.Ordinal))
            return new CredentialConflictException(CredentialConflictKind.RegistrationInUse, ex);
        if (sql.Message.Contains(CredentialsDbContext.OpenPerUserIndex, StringComparison.Ordinal))
            return new CredentialConflictException(CredentialConflictKind.OpenForUserAndProfession, ex);
        return null;
    }
}
