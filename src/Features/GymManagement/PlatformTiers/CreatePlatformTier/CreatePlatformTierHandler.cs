namespace ShapeUp.Features.GymManagement.PlatformTiers.CreatePlatformTier;

using FluentValidation;
using ShapeUp.Features.GymManagement.Shared.Abstractions;
using ShapeUp.Features.GymManagement.Shared.Entities;
using ShapeUp.Features.GymManagement.Shared.Errors;
using ShapeUp.Shared.Results;

public class CreatePlatformTierHandler(IPlatformTierRepository repository, IValidator<CreatePlatformTierCommand> validator)
{
    public async Task<Result<CreatePlatformTierResponse>> HandleAsync(CreatePlatformTierCommand command, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<CreatePlatformTierResponse>.Failure(
                CommonErrors.Validation(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage))));

        var existing = await repository.GetByNameAsync(command.Name, cancellationToken);
        if (existing != null)
            return Result<CreatePlatformTierResponse>.Failure(GymManagementErrors.PlatformTierNameAlreadyExists(command.Name));

        var tier = new PlatformTier
        {
            Name = command.Name,
            Description = command.Description,
            TargetRole = command.TargetRole,
            Price = command.Price,
            MaxClients = command.MaxClients,
            MaxTrainers = command.MaxTrainers
        };

        await repository.AddAsync(tier, cancellationToken);
        return Result<CreatePlatformTierResponse>.Success(
            new CreatePlatformTierResponse(tier.Id, tier.Name, tier.Description, tier.TargetRole, tier.Price, tier.MaxClients, tier.MaxTrainers));
    }
}

