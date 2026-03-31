using ShapeUp.Features.GymManagement.Shared.Abstractions;
using ShapeUp.Features.GymManagement.Shared.Entities;
using ShapeUp.Features.GymManagement.Shared.Errors;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.GymManagement.TrainerClients.AddTrainerClient;

using FluentValidation;

public class AddTrainerClientHandler(
    ITrainerClientRepository clientRepository,
    IGymClientRepository gymClientRepository,
    ITrainerPlanRepository planRepository,
    IUserPlatformRoleRepository roleRepository,
    IValidator<AddTrainerClientCommand> validator)
{
    public async Task<Result<AddTrainerClientResponse>> HandleAsync(AddTrainerClientCommand command, int trainerId, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<AddTrainerClientResponse>.Failure(CommonErrors.Validation(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage))));

        if (command.TrainerPlanId.HasValue)
        {
            var plan = await planRepository.GetByIdAsync(command.TrainerPlanId.Value, cancellationToken);
            if (plan is null)
                return Result<AddTrainerClientResponse>.Failure(GymManagementErrors.TrainerPlanNotFound(command.TrainerPlanId.Value));

            if (plan.TrainerId != trainerId)
                return Result<AddTrainerClientResponse>.Failure(GymManagementErrors.TrainerPlanDoesNotBelongToTrainer(command.TrainerPlanId.Value, trainerId));
        }

        var existing = await clientRepository.GetByTrainerAndClientAsync(trainerId, command.ClientId, cancellationToken);
        if (existing != null) return Result<AddTrainerClientResponse>.Failure(GymManagementErrors.ClientAlreadyUnderTrainer(command.ClientId, trainerId));

        var existingTrainerRelationship = await clientRepository.GetByClientIdAsync(command.ClientId, cancellationToken);
        if (existingTrainerRelationship != null)
            return Result<AddTrainerClientResponse>.Failure(
                GymManagementErrors.ClientAlreadyUnderTrainer(command.ClientId, existingTrainerRelationship.TrainerId));

        var existingGymRelationship = await gymClientRepository.GetByUserIdAsync(command.ClientId, cancellationToken);
        if (existingGymRelationship != null)
            return Result<AddTrainerClientResponse>.Failure(
                GymManagementErrors.ClientCannotBeTrainerAndGymClientAtSameTime(command.ClientId));

        var trainerClient = new TrainerClient
        {
            TrainerId = trainerId,
            ClientId = command.ClientId,
            TrainerPlanId = command.TrainerPlanId
        };
        await clientRepository.AddAsync(trainerClient, cancellationToken);

        var trainerClientRole = await roleRepository.GetByUserIdAndRoleAsync(command.ClientId, PlatformRoleType.Client, cancellationToken);
        if (trainerClientRole is null)
        {
            await roleRepository.AddAsync(new UserPlatformRole
            {
                UserId = command.ClientId,
                Role = PlatformRoleType.Client
            }, cancellationToken);
        }

        var independentRole = await roleRepository.GetByUserIdAndRoleAsync(command.ClientId, PlatformRoleType.IndependentClient, cancellationToken);
        if (independentRole != null)
            await roleRepository.DeleteAsync(independentRole.Id, cancellationToken);

        var gymClientRole = await roleRepository.GetByUserIdAndRoleAsync(command.ClientId, PlatformRoleType.GymClient, cancellationToken);
        if (gymClientRole != null)
            await roleRepository.DeleteAsync(gymClientRole.Id, cancellationToken);

        return Result<AddTrainerClientResponse>.Success(new AddTrainerClientResponse(trainerClient.Id, trainerClient.TrainerId, trainerClient.ClientId, trainerClient.TrainerPlanId, trainerClient.EnrolledAt));
    }
}

