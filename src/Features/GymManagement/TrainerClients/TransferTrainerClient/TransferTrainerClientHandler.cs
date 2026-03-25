namespace ShapeUp.Features.GymManagement.TrainerClients.TransferTrainerClient;

using ShapeUp.Features.GymManagement.Shared.Abstractions;
using ShapeUp.Features.GymManagement.Shared.Errors;
using ShapeUp.Shared.Results;

public class TransferTrainerClientHandler(
    ITrainerClientRepository clientRepository,
    ITrainerPlanRepository planRepository)
{
    public async Task<Result<TransferTrainerClientResponse>> HandleAsync(TransferTrainerClientCommand command, int currentTrainerId, CancellationToken cancellationToken)
    {
        var existing = await clientRepository.GetByTrainerAndClientAsync(currentTrainerId, command.ClientId, cancellationToken);
        if (existing is null)
            return Result<TransferTrainerClientResponse>.Failure(GymManagementErrors.TrainerClientNotFound(currentTrainerId, command.ClientId));

        var newPlan = await planRepository.GetByIdAsync(command.NewPlanId, cancellationToken);
        if (newPlan is null) return Result<TransferTrainerClientResponse>.Failure(GymManagementErrors.TrainerPlanNotFound(command.NewPlanId));
        if (newPlan.TrainerId != command.NewTrainerId) return Result<TransferTrainerClientResponse>.Failure(GymManagementErrors.TrainerPlanDoesNotBelongToTrainer(command.NewPlanId, command.NewTrainerId));

        await clientRepository.TransferAsync(existing.Id, command.NewTrainerId, command.NewPlanId, cancellationToken);
        return Result<TransferTrainerClientResponse>.Success(new TransferTrainerClientResponse(command.ClientId, currentTrainerId, command.NewTrainerId, command.NewPlanId));
    }
}

