namespace ShapeUp.Features.GymManagement.Gyms.CreateGym;

using FluentValidation;
using Shared.Abstractions;
using Shared.Entities;
using Shared.Errors;
using ShapeUp.Shared.Results;

public class CreateGymHandler(
    IGymRepository gymRepository,
    IPlatformTierRepository tierRepository,
    IUserPlatformRoleRepository roleRepository,
    IValidator<CreateGymCommand> validator)
{
    public async Task<Result<CreateGymResponse>> HandleAsync(CreateGymCommand command, int currentUserId, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<CreateGymResponse>.Failure(
                CommonErrors.Validation(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage))));

        if (command.PlatformTierId.HasValue)
        {
            var tier = await tierRepository.GetByIdAsync(command.PlatformTierId.Value, cancellationToken);
            if (tier is null)
                return Result<CreateGymResponse>.Failure(GymManagementErrors.PlatformTierNotFound(command.PlatformTierId.Value));
        }

        var gym = new Gym
        {
            OwnerId = currentUserId,
            Name = command.Name,
            Description = command.Description,
            Address = command.Address,
            PlatformTierId = command.PlatformTierId
        };

        await gymRepository.AddAsync(gym, cancellationToken);

        // Ensure the owner has the GymOwner role
        var ownerRole = await roleRepository.GetByUserIdAndRoleAsync(currentUserId, PlatformRoleType.GymOwner, cancellationToken);
        if (ownerRole is null)
        {
            await roleRepository.AddAsync(new UserPlatformRole
            {
                UserId = currentUserId,
                Role = PlatformRoleType.GymOwner,
                PlatformTierId = command.PlatformTierId
            }, cancellationToken);
        }

        return Result<CreateGymResponse>.Success(
            new CreateGymResponse(gym.Id, gym.OwnerId, gym.Name, gym.Description, gym.Address, gym.PlatformTierId, gym.CreatedAt));
    }
}

