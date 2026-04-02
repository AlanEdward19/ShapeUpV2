using FluentValidation;
using ShapeUp.Features.GymManagement.Shared.Abstractions;
using ShapeUp.Features.GymManagement.Shared.Errors;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.GymManagement.TrainerClients.DeactivateTrainerClientPlan;

public class DeactivateTrainerClientPlanHandler(
    ITrainerClientRepository trainerClientRepository,
    IValidator<DeactivateTrainerClientPlanCommand> validator)
{
    public async Task<Result<DeactivateTrainerClientPlanResponse>> HandleAsync(
        DeactivateTrainerClientPlanCommand command,
        int trainerId,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<DeactivateTrainerClientPlanResponse>.Failure(
                CommonErrors.Validation(string.Join("; ", validation.Errors.Select(x => x.ErrorMessage))));

        var existing = await trainerClientRepository.GetByTrainerAndClientAsync(trainerId, command.ClientId, cancellationToken);
        if (existing is null)
            return Result<DeactivateTrainerClientPlanResponse>.Failure(GymManagementErrors.TrainerClientNotFound(trainerId, command.ClientId));

        if (existing.TrainerPlanId is null)
            return Result<DeactivateTrainerClientPlanResponse>.Failure(
                CommonErrors.Conflict($"Client {command.ClientId} has no trainer plan assigned. Assign a plan before changing active status."));

        if (existing.IsActive == command.IsActive)
            return Result<DeactivateTrainerClientPlanResponse>.Failure(
                CommonErrors.Conflict($"Client {command.ClientId} plan status is already {(command.IsActive ? "active" : "inactive")}."));

        await trainerClientRepository.SetPlanStatusAsync(trainerId, command.ClientId, command.IsActive, cancellationToken);
        return Result<DeactivateTrainerClientPlanResponse>.Success(
            new DeactivateTrainerClientPlanResponse(trainerId, command.ClientId, command.IsActive, DateTime.UtcNow));
    }
}


