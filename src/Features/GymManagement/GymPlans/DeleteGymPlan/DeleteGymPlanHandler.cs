namespace ShapeUp.Features.GymManagement.GymPlans.DeleteGymPlan;

using ShapeUp.Features.GymManagement.Shared.Abstractions;
using ShapeUp.Features.GymManagement.Shared.Errors;
using ShapeUp.Shared.Results;

public class DeleteGymPlanHandler(
    IGymPlanRepository planRepository,
    IGymRepository gymRepository,
    IGymStaffRepository staffRepository)
{
    public async Task<Result> HandleAsync(DeleteGymPlanCommand command, int currentUserId, CancellationToken cancellationToken)
    {
        var gym = await gymRepository.GetByIdAsync(command.GymId, cancellationToken);
        if (gym is null) return Result.Failure(GymManagementErrors.GymNotFound(command.GymId));

        var canManage = await staffRepository.IsOwnerOrReceptionistAsync(command.GymId, currentUserId, gym.OwnerId, cancellationToken);
        if (!canManage) return Result.Failure(GymManagementErrors.NotGymOwnerOrReceptionist(currentUserId, command.GymId));

        var plan = await planRepository.GetByIdAsync(command.PlanId, cancellationToken);
        if (plan is null) return Result.Failure(GymManagementErrors.GymPlanNotFound(command.PlanId));
        if (plan.GymId != command.GymId) return Result.Failure(GymManagementErrors.GymPlanDoesNotBelongToGym(command.PlanId, command.GymId));

        await planRepository.DeleteAsync(command.PlanId, cancellationToken);
        return Result.Success();
    }
}

