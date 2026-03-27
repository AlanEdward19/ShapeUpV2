namespace ShapeUp.Features.GymManagement.PlatformTiers.UpdatePlatformTier;

using FluentValidation;
using Shared.Abstractions;
using Shared.Errors;
using ShapeUp.Shared.Results;

public class UpdatePlatformTierHandler(IPlatformTierRepository repository, IValidator<UpdatePlatformTierCommand> validator)
{
    public async Task<Result<UpdatePlatformTierResponse>> HandleAsync(UpdatePlatformTierCommand command, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<UpdatePlatformTierResponse>.Failure(
                CommonErrors.Validation(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage))));

        var tier = await repository.GetByIdAsync(command.Id, cancellationToken);
        if (tier is null)
            return Result<UpdatePlatformTierResponse>.Failure(GymManagementErrors.PlatformTierNotFound(command.Id));

        tier.Name = command.Name;
        tier.Description = command.Description;
        tier.TargetRole = command.TargetRole;
        tier.Price = command.Price;
        tier.MaxClients = command.MaxClients;
        tier.MaxTrainers = command.MaxTrainers;
        tier.IsActive = command.IsActive;

        await repository.UpdateAsync(tier, cancellationToken);
        return Result<UpdatePlatformTierResponse>.Success(
            new UpdatePlatformTierResponse(tier.Id, tier.Name, tier.Description, tier.TargetRole, tier.Price, tier.MaxClients, tier.MaxTrainers, tier.IsActive));
    }
}

