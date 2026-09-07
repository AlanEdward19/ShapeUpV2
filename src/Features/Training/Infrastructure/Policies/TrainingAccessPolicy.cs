namespace ShapeUp.Features.Training.Infrastructure.Policies;

using Microsoft.EntityFrameworkCore;
using ShapeUp.Features.GymManagement.Infrastructure.Data;
using ShapeUp.Features.GymManagement.Shared.Entities;
using ShapeUp.Features.Relationships.Shared.Abstractions;
using Shared.Abstractions;

/// <summary>
/// Native-authorization-model Phase 3 (RFC-001): self-access and professional-client relationship
/// checks now go through the capability model (self-access convention, IProfessionalClientRelationshipRepository)
/// instead of Scope strings. The two GymManagement-native checks (direct TrainerClients link,
/// gym-costaff-of-client) are kept exactly as they were and now run unconditionally -- they no
/// longer need a Scope to gate them, since the DB relationship itself is the source of truth
/// (matches the capability-model philosophy: derive permission from the actual relationship, not
/// an assigned flag). The compound "trainer works at gym X AND client is enrolled at gym X" case
/// is NOT modeled by CapabilityResolver yet (it needs two different users' memberships at the
/// same gym, not just the acting user's) -- deferred, tracked in STATE.md.
/// </summary>
public class TrainingAccessPolicy(
    GymManagementDbContext gymDbContext,
    IProfessionalClientRelationshipRepository relationshipRepository) : ITrainingAccessPolicy
{
    public async Task<bool> CanCreateWorkoutForAsync(int actorUserId, int targetUserId, CancellationToken cancellationToken)
    {
        if (actorUserId == targetUserId)
            return true;

        var relationship = await relationshipRepository.GetActiveAsync(actorUserId, targetUserId, "Training", cancellationToken);
        if (relationship is not null)
            return true;

        var isTrainerOfClient = await gymDbContext.TrainerClients
            .AsNoTracking()
            .AnyAsync(x => x.TrainerId == actorUserId
                           && x.ClientId == targetUserId
                           && x.IsActive
                           && x.TrainerPlanId != null,
                cancellationToken);

        if (isTrainerOfClient)
            return true;

        var allowedByGymRelation = await (from staff in gymDbContext.GymStaff.AsNoTracking()
            join client in gymDbContext.GymClients.AsNoTracking() on staff.GymId equals client.GymId
            where staff.UserId == actorUserId
                  && staff.IsActive
                  && staff.Role == GymStaffRole.Trainer
                  && client.UserId == targetUserId
                  && client.IsActive
            select staff.Id)
            .AnyAsync(cancellationToken);

        return allowedByGymRelation;
    }
}

