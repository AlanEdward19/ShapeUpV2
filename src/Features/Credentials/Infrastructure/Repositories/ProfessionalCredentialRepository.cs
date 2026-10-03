namespace ShapeUp.Features.Credentials.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Shared.Abstractions;
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
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ProfessionalCredential credential, CancellationToken cancellationToken)
    {
        context.ProfessionalCredentials.Update(credential);
        await context.SaveChangesAsync(cancellationToken);
    }
}
