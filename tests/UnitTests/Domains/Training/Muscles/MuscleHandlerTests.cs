namespace UnitTests.Domains.Training.Muscles;

using ShapeUp.Features.Training.Muscles.CreateMuscle;
using ShapeUp.Features.Training.Muscles.DeleteMuscle;
using ShapeUp.Features.Training.Muscles.GetMuscles;
using ShapeUp.Features.Training.Muscles.UpdateMuscle;
using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Entities;

public class MuscleHandlerTests
{
    private readonly Mock<IMuscleRepository> _muscleRepository = new();

    [Fact]
    public async Task CreateMuscleHandler_ValidCommand_PersistsMuscle()
    {
        _muscleRepository.Setup(x => x.AddAsync(It.IsAny<Muscle>(), default))
            .Callback<Muscle, CancellationToken>((muscle, _) => muscle.Id = 10)
            .Returns(Task.CompletedTask);

        var handler = new CreateMuscleHandler(_muscleRepository.Object, new CreateMuscleCommandValidator());
        var result = await handler.HandleAsync(new CreateMuscleCommand("Chest", "Peito"), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(10, result.Value!.Id);
        Assert.Equal("Chest", result.Value.Name);
        Assert.Equal("Peito", result.Value.NamePt);
    }

    [Fact]
    public async Task CreateMuscleHandler_InvalidCommand_ReturnsValidationFailure()
    {
        var handler = new CreateMuscleHandler(_muscleRepository.Object, new CreateMuscleCommandValidator());
        var result = await handler.HandleAsync(new CreateMuscleCommand(string.Empty, string.Empty), default);

        Assert.True(result.IsFailure);
        Assert.Equal("validation_error", result.Error!.Code);
    }

    [Fact]
    public async Task UpdateMuscleHandler_UnknownMuscle_ReturnsNotFound()
    {
        _muscleRepository.Setup(x => x.GetByIdAsync(3, default)).ReturnsAsync((Muscle?)null);

        var handler = new UpdateMuscleHandler(_muscleRepository.Object, new UpdateMuscleCommandValidator());
        var result = await handler.HandleAsync(new UpdateMuscleCommand(3, "Back", "Costas"), default);

        Assert.True(result.IsFailure);
        Assert.Equal("not_found", result.Error!.Code);
    }

    [Fact]
    public async Task DeleteMuscleHandler_UnknownMuscle_ReturnsNotFound()
    {
        _muscleRepository.Setup(x => x.GetByIdAsync(9, default)).ReturnsAsync((Muscle?)null);

        var handler = new DeleteMuscleHandler(_muscleRepository.Object);
        var result = await handler.HandleAsync(new DeleteMuscleCommand(9), default);

        Assert.True(result.IsFailure);
        Assert.Equal("not_found", result.Error!.Code);
    }

    [Fact]
    public async Task GetMusclesHandler_InvalidCursor_ReturnsValidationFailure()
    {
        var handler = new GetMusclesHandler(_muscleRepository.Object);
        var result = await handler.HandleAsync(new GetMusclesQuery("invalid-cursor", 20), default);

        Assert.True(result.IsFailure);
        Assert.Equal("validation_error", result.Error!.Code);
    }
}


