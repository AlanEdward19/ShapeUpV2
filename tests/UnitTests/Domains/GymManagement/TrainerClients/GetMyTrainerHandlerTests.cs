using ShapeUp.Features.GymManagement.Shared.Abstractions;
using ShapeUp.Features.GymManagement.Shared.Entities;
using ShapeUp.Features.GymManagement.TrainerClients.GetMyTrainer;

namespace UnitTests.Domains.GymManagement.TrainerClients;

public class GetMyTrainerHandlerTests
{
    private readonly Mock<ITrainerClientRepository> _repository = new();

    [Fact]
    public async Task HandleAsync_ClientWithTrainer_ReturnsTrainerId()
    {
        _repository
            .Setup(repo => repo.GetByClientIdAsync(9, default))
            .ReturnsAsync(new TrainerClient { Id = 1, TrainerId = 7, ClientId = 9 });

        var result = await new GetMyTrainerHandler(_repository.Object).HandleAsync(9, default);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.HasTrainer);
        Assert.Equal(7, result.Value.TrainerId);
    }

    [Fact]
    public async Task HandleAsync_ClientWithoutTrainer_ReturnsNoTrainer()
    {
        _repository
            .Setup(repo => repo.GetByClientIdAsync(9, default))
            .ReturnsAsync((TrainerClient?)null);

        var result = await new GetMyTrainerHandler(_repository.Object).HandleAsync(9, default);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.HasTrainer);
        Assert.Null(result.Value.TrainerId);
    }
}
