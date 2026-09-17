using ShapeUp.Features.Training.Exercises.GetExerciseEquivalents;
using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Entities;
using ShapeUp.Features.Training.Shared.Enums;

namespace UnitTests.Domains.Training.Exercises;

public class GetExerciseEquivalentsHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenExerciseMissing_ReturnsNotFound()
    {
        var exerciseRepository = new Mock<IExerciseRepository>();
        exerciseRepository.Setup(x => x.GetByIdAsync(9, It.IsAny<CancellationToken>())).ReturnsAsync((Exercise?)null);
        var equivalentRepository = new Mock<IExerciseEquivalentRepository>();

        var sut = new GetExerciseEquivalentsHandler(exerciseRepository.Object, equivalentRepository.Object);

        var result = await sut.HandleAsync(new GetExerciseEquivalentsQuery(9), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(404, result.Error!.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_WhenNoEquivalents_ReturnsEmptyArray()
    {
        var exerciseRepository = new Mock<IExerciseRepository>();
        exerciseRepository
            .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exercise { Id = 1, Name = "Bench", NamePt = "Supino" });
        var equivalentRepository = new Mock<IExerciseEquivalentRepository>();
        equivalentRepository
            .Setup(x => x.GetEquivalentsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var sut = new GetExerciseEquivalentsHandler(exerciseRepository.Object, equivalentRepository.Object);

        var result = await sut.HandleAsync(new GetExerciseEquivalentsQuery(1), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!);
    }

    [Fact]
    public async Task HandleAsync_WhenEquivalentsExist_ReturnsMappedResponses()
    {
        var exerciseRepository = new Mock<IExerciseRepository>();
        exerciseRepository
            .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exercise { Id = 1, Name = "Bench", NamePt = "Supino" });
        var equivalentRepository = new Mock<IExerciseEquivalentRepository>();
        equivalentRepository
            .Setup(x => x.GetEquivalentsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new Exercise
                {
                    Id = 2,
                    Name = "Push Up",
                    NamePt = "Flexao",
                    MuscleProfiles = [new ExerciseMuscleProfile { MuscleGroup = MuscleGroup.MiddleChest, ActivationPercent = 70 }],
                    Steps = [],
                    ExerciseEquipments = []
                }
            ]);

        var sut = new GetExerciseEquivalentsHandler(exerciseRepository.Object, equivalentRepository.Object);

        var result = await sut.HandleAsync(new GetExerciseEquivalentsQuery(1), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
        Assert.Equal(2, result.Value![0].Id);
        Assert.Equal("Push Up", result.Value[0].Name);
    }
}
