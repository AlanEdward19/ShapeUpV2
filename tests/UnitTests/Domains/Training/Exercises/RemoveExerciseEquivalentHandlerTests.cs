using ShapeUp.Features.Training.Exercises.RemoveExerciseEquivalent;
using ShapeUp.Features.Training.Shared.Abstractions;

namespace UnitTests.Domains.Training.Exercises;

public class RemoveExerciseEquivalentHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenValid_RemovesViaRepository()
    {
        var equivalentRepository = new Mock<IExerciseEquivalentRepository>();
        equivalentRepository
            .Setup(x => x.RemoveEquivalentAsync(1, 2, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = new RemoveExerciseEquivalentHandler(
            equivalentRepository.Object,
            new RemoveExerciseEquivalentCommandValidator());

        var result = await sut.HandleAsync(new RemoveExerciseEquivalentCommand(1, 2), CancellationToken.None);

        Assert.True(result.IsSuccess);
        equivalentRepository.Verify(x => x.RemoveEquivalentAsync(1, 2, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenCalledTwice_IsIdempotentSuccess()
    {
        var equivalentRepository = new Mock<IExerciseEquivalentRepository>();
        equivalentRepository
            .Setup(x => x.RemoveEquivalentAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = new RemoveExerciseEquivalentHandler(
            equivalentRepository.Object,
            new RemoveExerciseEquivalentCommandValidator());

        var first = await sut.HandleAsync(new RemoveExerciseEquivalentCommand(2, 1), CancellationToken.None);
        var second = await sut.HandleAsync(new RemoveExerciseEquivalentCommand(2, 1), CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        equivalentRepository.Verify(x => x.RemoveEquivalentAsync(2, 1, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}
