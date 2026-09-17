using ShapeUp.Features.Training.Exercises.SetExerciseEquivalent;
using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Entities;
using ShapeUp.Features.Training.Shared.Enums;

namespace UnitTests.Domains.Training.Exercises;

public class SetExerciseEquivalentHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenSelfEquivalent_ReturnsValidationError()
    {
        var sut = NewSut(out _, out var equivalentRepository);

        var result = await sut.HandleAsync(new SetExerciseEquivalentCommand(1, 1), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(400, result.Error!.StatusCode);
        equivalentRepository.Verify(
            x => x.SetEquivalentAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenExerciseMissing_ReturnsNotFound()
    {
        var exerciseRepository = new Mock<IExerciseRepository>();
        exerciseRepository.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((Exercise?)null);
        var equivalentRepository = new Mock<IExerciseEquivalentRepository>();
        var sut = new SetExerciseEquivalentHandler(
            exerciseRepository.Object,
            equivalentRepository.Object,
            new SetExerciseEquivalentCommandValidator());

        var result = await sut.HandleAsync(new SetExerciseEquivalentCommand(1, 2), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(404, result.Error!.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_WhenValidWithSharedMuscle_PersistsWithoutWarning()
    {
        var sut = NewSut(out _, out var equivalentRepository, sharedMuscle: true);

        var result = await sut.HandleAsync(new SetExerciseEquivalentCommand(1, 2), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.MuscleGroupOverlapWarning);
        equivalentRepository.Verify(x => x.SetEquivalentAsync(1, 2, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenNoSharedMuscle_ReturnsOverlapWarning()
    {
        var sut = NewSut(out _, out _, sharedMuscle: false);

        var result = await sut.HandleAsync(new SetExerciseEquivalentCommand(1, 2), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.MuscleGroupOverlapWarning);
    }

    private static SetExerciseEquivalentHandler NewSut(
        out Mock<IExerciseRepository> exerciseRepository,
        out Mock<IExerciseEquivalentRepository> equivalentRepository,
        bool sharedMuscle = true)
    {
        exerciseRepository = new Mock<IExerciseRepository>();
        exerciseRepository
            .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exercise
            {
                Id = 1,
                Name = "Bench",
                NamePt = "Supino",
                MuscleProfiles = [new ExerciseMuscleProfile { MuscleGroup = MuscleGroup.MiddleChest, ActivationPercent = 80 }]
            });
        exerciseRepository
            .Setup(x => x.GetByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exercise
            {
                Id = 2,
                Name = "Push Up",
                NamePt = "Flexao",
                MuscleProfiles =
                [
                    new ExerciseMuscleProfile
                    {
                        MuscleGroup = sharedMuscle ? MuscleGroup.MiddleChest : MuscleGroup.Quadriceps,
                        ActivationPercent = 70
                    }
                ]
            });

        equivalentRepository = new Mock<IExerciseEquivalentRepository>();
        equivalentRepository
            .Setup(x => x.SetEquivalentAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return new SetExerciseEquivalentHandler(
            exerciseRepository.Object,
            equivalentRepository.Object,
            new SetExerciseEquivalentCommandValidator());
    }
}
