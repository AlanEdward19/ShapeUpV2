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
}
