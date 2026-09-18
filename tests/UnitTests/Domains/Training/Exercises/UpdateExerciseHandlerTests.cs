using ShapeUp.Features.Training.Exercises.UpdateExercise;
using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Entities;
using ShapeUp.Features.Training.Shared.Enums;

namespace UnitTests.Domains.Training.Exercises;

public class UpdateExerciseHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenExerciseTypeChanged_UpdatesAndReturnsNewType()
    {
        var existing = new Exercise { Id = 1, Name = "Running", NamePt = "Corrida", ExerciseType = ExerciseType.WeightBased };
        var exerciseRepository = new Mock<IExerciseRepository>();
        exerciseRepository.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        exerciseRepository.Setup(x => x.UpdateAsync(It.IsAny<Exercise>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var equipmentRepository = new Mock<IEquipmentRepository>();

        var sut = new UpdateExerciseHandler(exerciseRepository.Object, equipmentRepository.Object, new UpdateExerciseCommandValidator());

        var result = await sut.HandleAsync(
            new UpdateExerciseCommand(1, "Running", "Corrida", null, null, [], [], null, ExerciseType.TimeBased),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(ExerciseType.TimeBased, result.Value!.ExerciseType);
        Assert.Equal(ExerciseType.TimeBased, existing.ExerciseType);
        exerciseRepository.Verify(
            x => x.UpdateAsync(It.Is<Exercise>(e => e.ExerciseType == ExerciseType.TimeBased), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void Validator_WhenExerciseTypeOutOfEnumRange_Fails()
    {
        var validator = new UpdateExerciseCommandValidator();
        var command = new UpdateExerciseCommand(1, "Running", "Corrida", null, null, [], [], null, (ExerciseType)99);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateExerciseCommand.ExerciseType));
    }
}
