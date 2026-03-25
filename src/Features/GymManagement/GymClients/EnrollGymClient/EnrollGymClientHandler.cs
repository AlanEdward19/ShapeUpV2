namespace ShapeUp.Features.GymManagement.GymClients.EnrollGymClient;

using FluentValidation;
using ShapeUp.Features.GymManagement.Shared.Abstractions;
using ShapeUp.Features.GymManagement.Shared.Entities;
using ShapeUp.Features.GymManagement.Shared.Errors;
using ShapeUp.Shared.Results;

public class EnrollGymClientHandler(
    IGymClientRepository clientRepository,
    IGymRepository gymRepository,
    IGymPlanRepository planRepository,
    IGymStaffRepository staffRepository,
    IValidator<EnrollGymClientCommand> validator)
{
    public async Task<Result<EnrollGymClientResponse>> HandleAsync(EnrollGymClientCommand command, int currentUserId, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<EnrollGymClientResponse>.Failure(CommonErrors.Validation(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage))));

        var gym = await gymRepository.GetByIdAsync(command.GymId, cancellationToken);
        if (gym is null) return Result<EnrollGymClientResponse>.Failure(GymManagementErrors.GymNotFound(command.GymId));

        var canManage = await staffRepository.IsOwnerOrReceptionistAsync(command.GymId, currentUserId, gym.OwnerId, cancellationToken);
        if (!canManage) return Result<EnrollGymClientResponse>.Failure(GymManagementErrors.NotGymOwnerOrReceptionist(currentUserId, command.GymId));

        var plan = await planRepository.GetByIdAsync(command.GymPlanId, cancellationToken);
        if (plan is null) return Result<EnrollGymClientResponse>.Failure(GymManagementErrors.GymPlanNotFound(command.GymPlanId));
        if (plan.GymId != command.GymId) return Result<EnrollGymClientResponse>.Failure(GymManagementErrors.GymPlanDoesNotBelongToGym(command.GymPlanId, command.GymId));

        var existing = await clientRepository.GetByGymAndUserAsync(command.GymId, command.UserId, cancellationToken);
        if (existing != null) return Result<EnrollGymClientResponse>.Failure(GymManagementErrors.UserAlreadyClientInGym(command.UserId, command.GymId));

        if (command.TrainerId.HasValue)
        {
            var trainer = await staffRepository.GetByIdAsync(command.TrainerId.Value, cancellationToken);
            if (trainer is null || trainer.GymId != command.GymId)
                return Result<EnrollGymClientResponse>.Failure(GymManagementErrors.GymStaffNotFound(command.GymId, command.TrainerId.Value));
            if (trainer.Role != GymStaffRole.Trainer)
                return Result<EnrollGymClientResponse>.Failure(GymManagementErrors.StaffMemberIsNotTrainer(command.TrainerId.Value));
        }

        var client = new GymClient { GymId = command.GymId, UserId = command.UserId, GymPlanId = command.GymPlanId, TrainerId = command.TrainerId };
        await clientRepository.AddAsync(client, cancellationToken);
        return Result<EnrollGymClientResponse>.Success(new EnrollGymClientResponse(client.Id, client.GymId, client.UserId, client.GymPlanId, client.TrainerId, client.EnrolledAt));
    }
}

