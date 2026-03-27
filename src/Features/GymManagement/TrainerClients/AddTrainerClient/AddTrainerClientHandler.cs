namespace ShapeUp.Features.GymManagement.TrainerClients.AddTrainerClient;

using FluentValidation;
using Shared.Abstractions;
using Shared.Entities;
using Shared.Errors;
using ShapeUp.Shared.Results;

public class AddTrainerClientHandler(
    ITrainerClientRepository clientRepository,
    ITrainerPlanRepository planRepository,
    IValidator<AddTrainerClientCommand> validator)
{
    public async Task<Result<AddTrainerClientResponse>> HandleAsync(AddTrainerClientCommand command, int trainerId, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<AddTrainerClientResponse>.Failure(CommonErrors.Validation(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage))));

        var plan = await planRepository.GetByIdAsync(command.TrainerPlanId, cancellationToken);
        if (plan is null) return Result<AddTrainerClientResponse>.Failure(GymManagementErrors.TrainerPlanNotFound(command.TrainerPlanId));
        if (plan.TrainerId != trainerId) return Result<AddTrainerClientResponse>.Failure(GymManagementErrors.TrainerPlanDoesNotBelongToTrainer(command.TrainerPlanId, trainerId));

        var existing = await clientRepository.GetByTrainerAndClientAsync(trainerId, command.ClientId, cancellationToken);
        if (existing != null) return Result<AddTrainerClientResponse>.Failure(GymManagementErrors.ClientAlreadyUnderTrainer(command.ClientId, trainerId));

        var trainerClient = new TrainerClient { TrainerId = trainerId, ClientId = command.ClientId, TrainerPlanId = command.TrainerPlanId };
        await clientRepository.AddAsync(trainerClient, cancellationToken);
        return Result<AddTrainerClientResponse>.Success(new AddTrainerClientResponse(trainerClient.Id, trainerClient.TrainerId, trainerClient.ClientId, trainerClient.TrainerPlanId, trainerClient.EnrolledAt));
    }
}

