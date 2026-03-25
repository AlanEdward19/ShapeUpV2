namespace ShapeUp.Features.GymManagement.PlatformTiers.DeletePlatformTier;

using ShapeUp.Features.GymManagement.Shared.Abstractions;
using ShapeUp.Features.GymManagement.Shared.Errors;
using ShapeUp.Shared.Results;

public class DeletePlatformTierHandler(IPlatformTierRepository repository)
{
    public async Task<Result> HandleAsync(DeletePlatformTierCommand command, CancellationToken cancellationToken)
    {
        var tier = await repository.GetByIdAsync(command.Id, cancellationToken);
        if (tier is null)
            return Result.Failure(GymManagementErrors.PlatformTierNotFound(command.Id));

        await repository.DeleteAsync(command.Id, cancellationToken);
        return Result.Success();
    }
}

