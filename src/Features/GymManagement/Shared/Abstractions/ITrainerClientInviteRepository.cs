namespace ShapeUp.Features.GymManagement.Shared.Abstractions;

using Entities;

public interface ITrainerClientInviteRepository
{
    Task<TrainerClientInvite?> GetByTokenHashAsync(string accessTokenHash, CancellationToken cancellationToken);
    Task<TrainerClientInvite?> GetActiveByTrainerAndEmailAsync(int trainerId, string inviteeEmail, CancellationToken cancellationToken);
    Task AddAsync(TrainerClientInvite invite, CancellationToken cancellationToken);
    Task UpdateAsync(TrainerClientInvite invite, CancellationToken cancellationToken);
}

