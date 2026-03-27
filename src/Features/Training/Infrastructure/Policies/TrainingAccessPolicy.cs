namespace ShapeUp.Features.Training.Infrastructure.Policies;

using Microsoft.EntityFrameworkCore;
using ShapeUp.Features.GymManagement.Infrastructure.Data;
using ShapeUp.Features.GymManagement.Shared.Entities;
using Shared.Abstractions;

public class TrainingAccessPolicy(GymManagementDbContext gymDbContext) : ITrainingAccessPolicy
{
    public async Task<bool> CanCreateWorkoutForAsync(int actorUserId, int targetUserId, string[] actorScopes, CancellationToken cancellationToken)
    {
        var scopeSet = actorScopes.ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (actorUserId == targetUserId && scopeSet.Contains("training:workouts:create:self"))
            return true;

        if (scopeSet.Contains("training:workouts:create:trainer"))
        {
            var isTrainerOfClient = await gymDbContext.TrainerClients
                .AsNoTracking()
                .AnyAsync(x => x.TrainerId == actorUserId && x.ClientId == targetUserId && x.IsActive, cancellationToken);

            if (isTrainerOfClient)
                return true;
        }

        if (scopeSet.Contains("training:workouts:create:gym_staff"))
        {
            var allowedByGymRelation = await (from staff in gymDbContext.GymStaff.AsNoTracking()
                join client in gymDbContext.GymClients.AsNoTracking() on staff.GymId equals client.GymId
                where staff.UserId == actorUserId
                      && staff.IsActive
                      && staff.Role == GymStaffRole.Trainer
                      && client.UserId == targetUserId
                      && client.IsActive
                select staff.Id)
                .AnyAsync(cancellationToken);

            if (allowedByGymRelation)
                return true;
        }

        return false;
    }
}

