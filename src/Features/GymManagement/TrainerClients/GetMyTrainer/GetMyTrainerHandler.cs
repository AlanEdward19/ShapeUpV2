using ShapeUp.Features.GymManagement.Shared.Abstractions;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.GymManagement.TrainerClients.GetMyTrainer;

/// <summary>Tells a client whether a trainer follows them; the apps use it to show or hide trainer-only UI.</summary>
public class GetMyTrainerHandler(ITrainerClientRepository repository)
{
    public async Task<Result<GetMyTrainerResponse>> HandleAsync(int clientId, CancellationToken cancellationToken)
    {
        var link = await repository.GetByClientIdAsync(clientId, cancellationToken);
        return Result<GetMyTrainerResponse>.Success(
            link is null ? new GetMyTrainerResponse(false, null) : new GetMyTrainerResponse(true, link.TrainerId));
    }
}
