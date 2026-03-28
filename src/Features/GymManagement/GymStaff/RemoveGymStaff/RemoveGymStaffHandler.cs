namespace ShapeUp.Features.GymManagement.GymStaff.RemoveGymStaff;

using Shared.Abstractions;
using Shared.Errors;
using ShapeUp.Shared.Results;

public class RemoveGymStaffHandler(IGymStaffRepository staffRepository, IGymRepository gymRepository)
{
    public async Task<Result> HandleAsync(RemoveGymStaffCommand command, int currentUserId, CancellationToken cancellationToken)
    {
        var gym = await gymRepository.GetByIdAsync(command.GymId, cancellationToken);
        if (gym is null) return Result.Failure(GymManagementErrors.GymNotFound(command.GymId));

        var canManage = await staffRepository.IsOwnerOrReceptionistAsync(command.GymId, currentUserId, gym.OwnerId, cancellationToken);
        if (!canManage) return Result.Failure(GymManagementErrors.NotGymOwnerOrReceptionist(currentUserId, command.GymId));

        var staff = await staffRepository.GetByIdAsync(command.StaffId, cancellationToken);
        if (staff is null || staff.GymId != command.GymId)
            return Result.Failure(GymManagementErrors.GymStaffNotFound(command.GymId, command.StaffId));

        await staffRepository.RemoveAsync(command.StaffId, cancellationToken);
        return Result.Success();
    }
}

