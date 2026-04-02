using FluentValidation;
using ShapeUp.Features.GymManagement.Shared.Abstractions;
using ShapeUp.Features.GymManagement.Shared.Errors;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.GymManagement.TrainerClients.UnassignTrainerClient;

public class UnassignTrainerClientHandler(
    ITrainerClientRepository trainerClientRepository,
    IValidator<UnassignTrainerClientCommand> validator)
{
    public async Task<Result<UnassignTrainerClientResponse>> HandleAsync(
        UnassignTrainerClientCommand command,
        int trainerId,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<UnassignTrainerClientResponse>.Failure(
                CommonErrors.Validation(string.Join("; ", validation.Errors.Select(x => x.ErrorMessage))));

        var existing = await trainerClientRepository.GetByTrainerAndClientAsync(trainerId, command.ClientId, cancellationToken);
        if (existing is null)
            return Result<UnassignTrainerClientResponse>.Failure(GymManagementErrors.TrainerClientNotFound(trainerId, command.ClientId));

        await trainerClientRepository.UnassignAsync(trainerId, command.ClientId, cancellationToken);
        return Result<UnassignTrainerClientResponse>.Success(
            new UnassignTrainerClientResponse(trainerId, command.ClientId, DateTime.UtcNow));
    }
}

