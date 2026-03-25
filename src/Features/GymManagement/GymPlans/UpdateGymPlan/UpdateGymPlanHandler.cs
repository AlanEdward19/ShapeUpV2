namespace ShapeUp.Features.GymManagement.GymPlans.UpdateGymPlan;

using FluentValidation;
using ShapeUp.Features.GymManagement.Shared.Abstractions;
using ShapeUp.Features.GymManagement.Shared.Errors;
using ShapeUp.Shared.Results;

public class UpdateGymPlanHandler(
    IGymPlanRepository planRepository,
    IGymRepository gymRepository,
    IGymStaffRepository staffRepository,
    IValidator<UpdateGymPlanCommand> validator)
{
    public async Task<Result<UpdateGymPlanResponse>> HandleAsync(UpdateGymPlanCommand command, int currentUserId, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<UpdateGymPlanResponse>.Failure(CommonErrors.Validation(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage))));

        var gym = await gymRepository.GetByIdAsync(command.GymId, cancellationToken);
        if (gym is null) return Result<UpdateGymPlanResponse>.Failure(GymManagementErrors.GymNotFound(command.GymId));

        var canManage = await staffRepository.IsOwnerOrReceptionistAsync(command.GymId, currentUserId, gym.OwnerId, cancellationToken);
        if (!canManage) return Result<UpdateGymPlanResponse>.Failure(GymManagementErrors.NotGymOwnerOrReceptionist(currentUserId, command.GymId));

        var plan = await planRepository.GetByIdAsync(command.PlanId, cancellationToken);
        if (plan is null) return Result<UpdateGymPlanResponse>.Failure(GymManagementErrors.GymPlanNotFound(command.PlanId));
        if (plan.GymId != command.GymId) return Result<UpdateGymPlanResponse>.Failure(GymManagementErrors.GymPlanDoesNotBelongToGym(command.PlanId, command.GymId));

        plan.Name = command.Name;
        plan.Description = command.Description;
        plan.Price = command.Price;
        plan.DurationDays = command.DurationDays;
        plan.IsActive = command.IsActive;

        await planRepository.UpdateAsync(plan, cancellationToken);
        return Result<UpdateGymPlanResponse>.Success(new UpdateGymPlanResponse(plan.Id, plan.GymId, plan.Name, plan.Description, plan.Price, plan.DurationDays, plan.IsActive));
    }
}

