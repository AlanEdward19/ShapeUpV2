using ShapeUp.Features.Training.Exercises.CreateExercise;
using ShapeUp.Features.Training.Exercises.Shared.Dtos;
using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Entities;
using ShapeUp.Features.Training.Shared.Enums;

namespace UnitTests.Domains.Training.Exercises;

public class CreateExerciseHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenExerciseTypeIsTimeBased_PersistsAndReturnsTimeBased()
    {
        var sut = NewSut(out var exerciseRepository);

        var result = await sut.HandleAsync(
            new CreateExerciseCommand(
                "Running",
                "Corrida",
                null,
                null,
                [],
                [],
                null,
                ExerciseType.TimeBased),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(ExerciseType.TimeBased, result.Value!.ExerciseType);
        exerciseRepository.Verify(
            x => x.AddAsync(It.Is<Exercise>(e => e.ExerciseType == ExerciseType.TimeBased), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenExerciseTypeOmitted_DefaultsToWeightBased()
    {
        var sut = NewSut(out var exerciseRepository);

        var result = await sut.HandleAsync(
            new CreateExerciseCommand("Bench Press", "Supino", null, null, [], [], null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(ExerciseType.WeightBased, result.Value!.ExerciseType);
        exerciseRepository.Verify(
            x => x.AddAsync(It.Is<Exercise>(e => e.ExerciseType == ExerciseType.WeightBased), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenTwoMusclesProvided_PersistsBothAndReturnsBothInResponse()
    {
        var sut = NewSut(out var exerciseRepository);

        var result = await sut.HandleAsync(
            new CreateExerciseCommand(
                "Incline Press",
                "Supino Inclinado",
                null,
                null,
                [
                    new ExerciseMuscleDto(MuscleGroup.MiddleChest, 80),
                    new ExerciseMuscleDto(MuscleGroup.Triceps, 55)
                ],
                [],
                null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Muscles.Length);
        Assert.Contains(result.Value.Muscles, m => m.MuscleGroup == MuscleGroup.MiddleChest);
        Assert.Contains(result.Value.Muscles, m => m.MuscleGroup == MuscleGroup.Triceps);
        exerciseRepository.Verify(
            x => x.AddAsync(It.Is<Exercise>(e => e.MuscleProfiles.Count == 2), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenSingleMuscleProvided_PersistsOneProfile()
    {
        var sut = NewSut(out var exerciseRepository);

        var result = await sut.HandleAsync(
            new CreateExerciseCommand(
                "Cable Fly",
                "Crucifixo na Polia",
                null,
                null,
                [new ExerciseMuscleDto(MuscleGroup.MiddleChest, 85)],
                [],
                null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Muscles);
        Assert.Equal(MuscleGroup.MiddleChest, result.Value.Muscles[0].MuscleGroup);
        exerciseRepository.Verify(
            x => x.AddAsync(It.Is<Exercise>(e => e.MuscleProfiles.Count == 1), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void Validator_WhenExerciseTypeOutOfEnumRange_Fails()
    {
        var validator = new CreateExerciseCommandValidator();
        var command = new CreateExerciseCommand("Bench Press", "Supino", null, null, [], [], null, (ExerciseType)99);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateExerciseCommand.ExerciseType));
    }

    private static CreateExerciseHandler NewSut(out Mock<IExerciseRepository> exerciseRepository)
    {
        Exercise? captured = null;
        exerciseRepository = new Mock<IExerciseRepository>();
        exerciseRepository
            .Setup(x => x.AddAsync(It.IsAny<Exercise>(), It.IsAny<CancellationToken>()))
            .Callback<Exercise, CancellationToken>((exercise, _) => captured = exercise)
            .Returns(Task.CompletedTask);
        exerciseRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => captured);

        var equipmentRepository = new Mock<IEquipmentRepository>();

        return new CreateExerciseHandler(exerciseRepository.Object, equipmentRepository.Object, new CreateExerciseCommandValidator());
    }
}
