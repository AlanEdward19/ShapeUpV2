namespace ShapeUp.Features.Relationships.Shared.Abstractions;

using Entities;

public interface IProfessionalClientInviteRepository
{
    Task AddAsync(ProfessionalClientInvite invite, CancellationToken cancellationToken);

    Task<ProfessionalClientInvite?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken);

    Task UpdateAsync(ProfessionalClientInvite invite, CancellationToken cancellationToken);
}
