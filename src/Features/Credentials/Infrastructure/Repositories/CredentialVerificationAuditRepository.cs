namespace ShapeUp.Features.Credentials.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Shared.Abstractions;
using Shared.Data;
using Shared.Entities;

public class CredentialVerificationAuditRepository(CredentialsDbContext context) : ICredentialVerificationAuditRepository
{
    public async Task AddAsync(CredentialVerificationAudit audit, CancellationToken cancellationToken)
    {
        await context.CredentialVerificationAudits.AddAsync(audit, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CredentialVerificationAudit>> GetByCredentialAsync(int credentialId, CancellationToken cancellationToken) =>
        await context.CredentialVerificationAudits
            .AsNoTracking()
            .Where(x => x.CredentialId == credentialId)
            .OrderByDescending(x => x.Id)
            .ToListAsync(cancellationToken);
}
