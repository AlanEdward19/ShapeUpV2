namespace ShapeUp.Features.Relationships.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Shared.Abstractions;
using Shared.Data;
using Shared.Entities;

public class ProfessionalClientInviteRepository(RelationshipsDbContext context) : IProfessionalClientInviteRepository
{
    public async Task AddAsync(ProfessionalClientInvite invite, CancellationToken cancellationToken)
    {
        await context.Set<ProfessionalClientInvite>().AddAsync(invite, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public Task<ProfessionalClientInvite?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        context.Set<ProfessionalClientInvite>()
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

    public async Task UpdateAsync(ProfessionalClientInvite invite, CancellationToken cancellationToken)
    {
        context.Set<ProfessionalClientInvite>().Update(invite);
        await context.SaveChangesAsync(cancellationToken);
    }
}
