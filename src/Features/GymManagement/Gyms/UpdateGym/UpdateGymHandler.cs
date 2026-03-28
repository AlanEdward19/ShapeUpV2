namespace ShapeUp.Features.GymManagement.Gyms.UpdateGym;

using FluentValidation;
using Shared.Abstractions;
using Shared.Errors;
using ShapeUp.Shared.Results;

public class UpdateGymHandler(
    IGymRepository gymRepository,
    IPlatformTierRepository tierRepository,
    IGymStaffRepository staffRepository,
    IValidator<UpdateGymCommand> validator)
{
    public async Task<Result<UpdateGymResponse>> HandleAsync(UpdateGymCommand command, int currentUserId, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<UpdateGymResponse>.Failure(CommonErrors.Validation(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage))));

        var gym = await gymRepository.GetByIdAsync(command.GymId, cancellationToken);
        if (gym is null)
            return Result<UpdateGymResponse>.Failure(GymManagementErrors.GymNotFound(command.GymId));

        var isStaff = await staffRepository.IsStaffAsync(command.GymId, currentUserId, cancellationToken);
        if (gym.OwnerId != currentUserId && !isStaff)
            return Result<UpdateGymResponse>.Failure(GymManagementErrors.NotGymOwnerOrStaff(currentUserId, command.GymId));

        if (command.PlatformTierId.HasValue)
        {
            var tier = await tierRepository.GetByIdAsync(command.PlatformTierId.Value, cancellationToken);
            if (tier is null)
                return Result<UpdateGymResponse>.Failure(GymManagementErrors.PlatformTierNotFound(command.PlatformTierId.Value));
        }

        gym.Name = command.Name;
        gym.Description = command.Description;
        gym.Address = command.Address;
        gym.PlatformTierId = command.PlatformTierId;
        gym.IsActive = command.IsActive;

        await gymRepository.UpdateAsync(gym, cancellationToken);

        return Result<UpdateGymResponse>.Success(new UpdateGymResponse(
            gym.Id,
            gym.OwnerId,
            gym.Name,
            gym.Description,
            gym.Address,
            gym.PlatformTierId,
            gym.IsActive,
            gym.UpdatedAt));
    }
}

