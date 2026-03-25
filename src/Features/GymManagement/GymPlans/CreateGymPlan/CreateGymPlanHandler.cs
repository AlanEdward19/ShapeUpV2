namespace ShapeUp.Features.GymManagement.GymPlans.CreateGymPlan;

using FluentValidation;
using ShapeUp.Features.GymManagement.Shared.Abstractions;
using ShapeUp.Features.GymManagement.Shared.Entities;
using ShapeUp.Features.GymManagement.Shared.Errors;
using ShapeUp.Shared.Results;

public class CreateGymPlanHandler(
    IGymPlanRepository planRepository,
    IGymRepository gymRepository,
    IGymStaffRepository staffRepository,
    IValidator<CreateGymPlanCommand> validator)
{
    public async Task<Result<CreateGymPlanResponse>> HandleAsync(CreateGymPlanCommand command, int currentUserId, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<CreateGymPlanResponse>.Failure(
                CommonErrors.Validation(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage))));

        var gym = await gymRepository.GetByIdAsync(command.GymId, cancellationToken);
        if (gym is null) return Result<CreateGymPlanResponse>.Failure(GymManagementErrors.GymNotFound(command.GymId));

        var canManage = await staffRepository.IsOwnerOrReceptionistAsync(command.GymId, currentUserId, gym.OwnerId, cancellationToken);
        if (!canManage) return Result<CreateGymPlanResponse>.Failure(GymManagementErrors.NotGymOwnerOrReceptionist(currentUserId, command.GymId));

        var plan = new GymPlan { GymId = command.GymId, Name = command.Name, Description = command.Description, Price = command.Price, DurationDays = command.DurationDays };
        await planRepository.AddAsync(plan, cancellationToken);

        return Result<CreateGymPlanResponse>.Success(new CreateGymPlanResponse(plan.Id, plan.GymId, plan.Name, plan.Description, plan.Price, plan.DurationDays));
    }
}

