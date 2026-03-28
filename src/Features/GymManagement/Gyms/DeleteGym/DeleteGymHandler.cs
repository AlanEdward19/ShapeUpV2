namespace ShapeUp.Features.GymManagement.Gyms.DeleteGym;

using Shared.Abstractions;
using Shared.Errors;
using ShapeUp.Shared.Results;

public class DeleteGymHandler(IGymRepository gymRepository, IGymStaffRepository staffRepository)
{
    public async Task<Result> HandleAsync(DeleteGymCommand command, int currentUserId, CancellationToken cancellationToken)
    {
        var gym = await gymRepository.GetByIdAsync(command.GymId, cancellationToken);
        if (gym is null)
            return Result.Failure(GymManagementErrors.GymNotFound(command.GymId));

        var isStaff = await staffRepository.IsStaffAsync(command.GymId, currentUserId, cancellationToken);
        if (gym.OwnerId != currentUserId && !isStaff)
            return Result.Failure(GymManagementErrors.NotGymOwnerOrStaff(currentUserId, command.GymId));

        await gymRepository.DeleteAsync(command.GymId, cancellationToken);
        return Result.Success();
    }
}

