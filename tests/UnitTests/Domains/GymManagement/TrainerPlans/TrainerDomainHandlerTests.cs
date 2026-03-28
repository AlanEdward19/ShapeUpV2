using ShapeUp.Features.GymManagement.Shared.Abstractions;
using ShapeUp.Features.GymManagement.Shared.Entities;
using ShapeUp.Features.GymManagement.TrainerClients.AddTrainerClient;
using ShapeUp.Features.GymManagement.TrainerClients.TransferTrainerClient;
using ShapeUp.Features.GymManagement.TrainerPlans.CreateTrainerPlan;
using ShapeUp.Features.GymManagement.TrainerPlans.DeleteTrainerPlan;

namespace UnitTests.Domains.GymManagement.TrainerPlans;

public class TrainerDomainHandlerTests
{
    private readonly Mock<ITrainerPlanRepository> _planRepo = new();
    private readonly Mock<ITrainerClientRepository> _clientRepo = new();

    public static IEnumerable<object[]> ValidPlanCases =>
    [
        ["Starter", null, 59.90, 30],
        ["Full Body", "Detailed", 99.90, 60],
        ["Advance", "Elite", 149.90, 90],
    ];

    [Theory]
    [MemberData(nameof(ValidPlanCases))]
    public async Task CreateTrainerPlanHandler_ValidCommand_ReturnsSuccess(string name, string? desc, decimal price, int days)
    {
        _planRepo.Setup(r => r.AddAsync(It.IsAny<TrainerPlan>(), default))
                 .Callback<TrainerPlan, CancellationToken>((p, _) => p.Id = 1)
                 .Returns(Task.CompletedTask);

        var handler = new CreateTrainerPlanHandler(_planRepo.Object, new CreateTrainerPlanValidator());
        var result = await handler.HandleAsync(new CreateTrainerPlanCommand(name, desc, price, days), trainerId: 5, default);

        Assert.True(result.IsSuccess);
        Assert.Equal(name, result.Value!.Name);
        Assert.Equal(5, result.Value.TrainerId);
    }

    public static IEnumerable<object[]> InvalidPlanCases =>
    [
        ["", null, 50m, 30],
        ["Valid", null, -1m, 30],
        ["Valid", null, 50m, 0],
    ];

    [Theory]
    [MemberData(nameof(InvalidPlanCases))]
    public async Task CreateTrainerPlanHandler_Invalid_ReturnsValidationError(string name, string? desc, decimal price, int days)
    {
        var handler = new CreateTrainerPlanHandler(_planRepo.Object, new CreateTrainerPlanValidator());
        var result = await handler.HandleAsync(new CreateTrainerPlanCommand(name, desc, price, days), trainerId: 1, default);

        Assert.True(result.IsFailure);
        Assert.Equal("validation_error", result.Error!.Code);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    public async Task DeleteTrainerPlanHandler_OwnPlan_ReturnsSuccess(int planId, int trainerId)
    {
        _planRepo.Setup(r => r.GetByIdAsync(planId, default)).ReturnsAsync(new TrainerPlan { Id = planId, TrainerId = trainerId, Name = "X" });
        _planRepo.Setup(r => r.DeleteAsync(planId, default)).Returns(Task.CompletedTask);

        var handler = new DeleteTrainerPlanHandler(_planRepo.Object);
        var result = await handler.HandleAsync(new DeleteTrainerPlanCommand(planId, trainerId), default);

        Assert.True(result.IsSuccess);
        _planRepo.Verify(r => r.DeleteAsync(planId, default), Times.Once);
    }

    [Theory]
    [InlineData(1, 2)]
    [InlineData(5, 9)]
    public async Task DeleteTrainerPlanHandler_NotOwner_ReturnsForbidden(int planId, int wrongTrainerId)
    {
        _planRepo.Setup(r => r.GetByIdAsync(planId, default)).ReturnsAsync(new TrainerPlan { Id = planId, TrainerId = 99, Name = "X" });

        var handler = new DeleteTrainerPlanHandler(_planRepo.Object);
        var result = await handler.HandleAsync(new DeleteTrainerPlanCommand(planId, wrongTrainerId), default);

        Assert.True(result.IsFailure);
        Assert.Equal("validation_error", result.Error!.Code);
    }

    [Theory]
    [InlineData(1, 5, 10)]
    [InlineData(2, 6, 11)]
    public async Task AddTrainerClientHandler_ValidInput_ReturnsSuccess(int clientId, int trainerId, int planId)
    {
        _planRepo.Setup(r => r.GetByIdAsync(planId, default)).ReturnsAsync(new TrainerPlan { Id = planId, TrainerId = trainerId, Name = "P" });
        _clientRepo.Setup(r => r.GetByTrainerAndClientAsync(trainerId, clientId, default)).ReturnsAsync((TrainerClient?)null);
        _clientRepo.Setup(r => r.AddAsync(It.IsAny<TrainerClient>(), default))
                   .Callback<TrainerClient, CancellationToken>((c, _) => c.Id = 1)
                   .Returns(Task.CompletedTask);

        var handler = new AddTrainerClientHandler(_clientRepo.Object, _planRepo.Object, new AddTrainerClientValidator());
        var result = await handler.HandleAsync(new AddTrainerClientCommand(clientId, planId), trainerId, default);

        Assert.True(result.IsSuccess);
        Assert.Equal(clientId, result.Value!.ClientId);
        Assert.Equal(trainerId, result.Value.TrainerId);
    }

    [Theory]
    [InlineData(1, 5, 10)]
    [InlineData(2, 6, 11)]
    public async Task AddTrainerClientHandler_AlreadyExists_ReturnsConflict(int clientId, int trainerId, int planId)
    {
        _planRepo.Setup(r => r.GetByIdAsync(planId, default)).ReturnsAsync(new TrainerPlan { Id = planId, TrainerId = trainerId, Name = "P" });
        _clientRepo.Setup(r => r.GetByTrainerAndClientAsync(trainerId, clientId, default))
                   .ReturnsAsync(new TrainerClient { Id = 99, TrainerId = trainerId, ClientId = clientId, TrainerPlanId = planId });

        var handler = new AddTrainerClientHandler(_clientRepo.Object, _planRepo.Object, new AddTrainerClientValidator());
        var result = await handler.HandleAsync(new AddTrainerClientCommand(clientId, planId), trainerId, default);

        Assert.True(result.IsFailure);
        Assert.Equal("conflict", result.Error!.Code);
    }

    [Theory]
    [InlineData(3, 1, 2, 20)]
    [InlineData(4, 1, 3, 21)]
    public async Task TransferTrainerClientHandler_ValidTransfer_ReturnsSuccess(int clientId, int currentTrainerId, int newTrainerId, int newPlanId)
    {
        _clientRepo.Setup(r => r.GetByTrainerAndClientAsync(currentTrainerId, clientId, default))
                   .ReturnsAsync(new TrainerClient { Id = 5, TrainerId = currentTrainerId, ClientId = clientId, TrainerPlanId = 10 });
        _planRepo.Setup(r => r.GetByIdAsync(newPlanId, default)).ReturnsAsync(new TrainerPlan { Id = newPlanId, TrainerId = newTrainerId, Name = "P" });
        _clientRepo.Setup(r => r.TransferAsync(5, newTrainerId, newPlanId, default)).Returns(Task.CompletedTask);

        var handler = new TransferTrainerClientHandler(_clientRepo.Object, _planRepo.Object);
        var result = await handler.HandleAsync(new TransferTrainerClientCommand(clientId, newTrainerId, newPlanId), currentTrainerId, default);

        Assert.True(result.IsSuccess);
        Assert.Equal(newTrainerId, result.Value!.NewTrainerId);
    }
}

