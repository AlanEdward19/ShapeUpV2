namespace ShapeUp.Features.GymManagement.GymClients.AssignClientTrainer;

using ShapeUp.Features.GymManagement.Shared.Abstractions;
using ShapeUp.Features.GymManagement.Shared.Entities;
using ShapeUp.Features.GymManagement.Shared.Errors;
using ShapeUp.Shared.Results;

public class AssignClientTrainerHandler(
    IGymClientRepository clientRepository,
    IGymRepository gymRepository,
    IGymStaffRepository staffRepository)
{
    public async Task<Result<AssignClientTrainerResponse>> HandleAsync(AssignClientTrainerCommand command, int currentUserId, CancellationToken cancellationToken)
    {
        var gym = await gymRepository.GetByIdAsync(command.GymId, cancellationToken);
        if (gym is null) return Result<AssignClientTrainerResponse>.Failure(GymManagementErrors.GymNotFound(command.GymId));

        var canManage = await staffRepository.IsOwnerOrReceptionistAsync(command.GymId, currentUserId, gym.OwnerId, cancellationToken);
        var isTrainer = await staffRepository.IsStaffAsync(command.GymId, currentUserId, cancellationToken);
        if (!canManage && !isTrainer)
            return Result<AssignClientTrainerResponse>.Failure(GymManagementErrors.NotGymOwnerOrReceptionist(currentUserId, command.GymId));

        var client = await clientRepository.GetByIdAsync(command.ClientId, cancellationToken);
        if (client is null || client.GymId != command.GymId)
            return Result<AssignClientTrainerResponse>.Failure(GymManagementErrors.GymClientNotFound(command.GymId, command.ClientId));

        if (command.TrainerId.HasValue)
        {
            var trainer = await staffRepository.GetByIdAsync(command.TrainerId.Value, cancellationToken);
            if (trainer is null || trainer.GymId != command.GymId)
                return Result<AssignClientTrainerResponse>.Failure(GymManagementErrors.GymStaffNotFound(command.GymId, command.TrainerId.Value));
            if (trainer.Role != GymStaffRole.Trainer)
                return Result<AssignClientTrainerResponse>.Failure(GymManagementErrors.StaffMemberIsNotTrainer(command.TrainerId.Value));
        }

        await clientRepository.AssignTrainerAsync(command.ClientId, command.TrainerId, cancellationToken);
        return Result<AssignClientTrainerResponse>.Success(new AssignClientTrainerResponse(command.ClientId, command.GymId, command.TrainerId));
    }
}

