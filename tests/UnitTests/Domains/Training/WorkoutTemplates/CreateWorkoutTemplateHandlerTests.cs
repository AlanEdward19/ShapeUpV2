using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Documents;
using ShapeUp.Features.Training.Shared.Entities;
using ShapeUp.Features.Training.Shared.Enums;
using ShapeUp.Features.Training.WorkoutTemplates.CreateWorkoutTemplate;
using ShapeUp.Features.Training.Workouts.Shared.Dtos;
using ShapeUp.Features.Training.Workouts.Shared.ValueObjects;

namespace UnitTests.Domains.Training.WorkoutTemplates;

public class CreateWorkoutTemplateHandlerTests
{
    private static WorkoutSetValueObject StraightSet(int reps = 10, int rest = 90, IntensityDto? intensity = null) =>
        new(reps, 20, LoadUnit.Kg, SetType.Working, Technique.Straight, intensity ?? new IntensityDto(IntensityType.Rpe, 8), rest);

    [Fact]
    public async Task HandleAsync_WhenValid_CreatesWorkoutTemplate()
    {
        var sut = NewSut(out var templateRepository, out _);
        WorkoutTemplateDocument? capturedTemplate = null;
        templateRepository.Setup(x => x.AddAsync(It.IsAny<WorkoutTemplateDocument>(), It.IsAny<CancellationToken>()))
            .Callback<WorkoutTemplateDocument, CancellationToken>((template, _) => capturedTemplate = template)
            .Returns(Task.CompletedTask);

        var command = ValidCommandWith(new BlockDto(BlockType.Straight, [new WorkoutExerciseDto(1, [StraightSet()])]));

        var result = await sut.HandleAsync(command, 10, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(BlockType.Straight, capturedTemplate!.Blocks[0].Type);
    }

    [Fact]
    public async Task HandleAsync_WhenSupersetHasOnlyOneExercise_ReturnsValidationError()
    {
        var sut = NewSut(out _, out _);
        var command = ValidCommandWith(new BlockDto(BlockType.Superset, [new WorkoutExerciseDto(1, [StraightSet()])]));

        var result = await sut.HandleAsync(command, 10, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(400, result.Error!.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_WhenAmrapMissingTimeCap_ReturnsValidationError()
    {
        var sut = NewSut(out _, out _);
        var setNoRest = new WorkoutSetValueObject(10, 20, LoadUnit.Kg, SetType.Working, Technique.Straight, null, null);
        var command = ValidCommandWith(new BlockDto(BlockType.Amrap, [new WorkoutExerciseDto(1, [setNoRest])]));

        var result = await sut.HandleAsync(command, 10, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(400, result.Error!.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_WhenEmomMissingIntervalOrRounds_ReturnsValidationError()
    {
        var sut = NewSut(out _, out _);
        var setNoRest = new WorkoutSetValueObject(10, 20, LoadUnit.Kg, SetType.Working, Technique.Straight, null, null);
        var command = ValidCommandWith(new BlockDto(BlockType.Emom, [new WorkoutExerciseDto(1, [setNoRest])]));

        var result = await sut.HandleAsync(command, 10, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(400, result.Error!.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_WhenRestSecondsSetOnNonStraightBlock_ReturnsValidationError()
    {
        var sut = NewSut(out _, out _);
        var command = ValidCommandWith(new BlockDto(BlockType.Superset, [
            new WorkoutExerciseDto(1, [StraightSet()]),
            new WorkoutExerciseDto(1, [StraightSet()])
        ]));

        var result = await sut.HandleAsync(command, 10, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(400, result.Error!.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_WhenValidAmrapAndEmomBlocks_CreatesWorkoutTemplate()
    {
        var sut = NewSut(out var templateRepository, out _);
        WorkoutTemplateDocument? capturedTemplate = null;
        templateRepository.Setup(x => x.AddAsync(It.IsAny<WorkoutTemplateDocument>(), It.IsAny<CancellationToken>()))
            .Callback<WorkoutTemplateDocument, CancellationToken>((template, _) => capturedTemplate = template)
            .Returns(Task.CompletedTask);

        var noRestSet = new WorkoutSetValueObject(null, 20, LoadUnit.Kg, SetType.Working, Technique.Straight, null, null);
        var command = new CreateWorkoutTemplateCommand(
            "Push Day",
            null,
            4,
            "Strength",
            Difficulty.Hard,
            [
                new BlockDto(BlockType.Amrap, [new WorkoutExerciseDto(1, [noRestSet])], TimeCapSeconds: 600),
                new BlockDto(BlockType.Emom, [new WorkoutExerciseDto(1, [noRestSet]), new WorkoutExerciseDto(1, [noRestSet])], IntervalSeconds: 60, TotalRounds: 10)
            ]);

        var result = await sut.HandleAsync(command, 10, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(600, capturedTemplate!.Blocks[0].TimeCapSeconds);
        Assert.Equal(60, capturedTemplate.Blocks[1].IntervalSeconds);
        Assert.Equal(10, capturedTemplate.Blocks[1].TotalRounds);
    }

    private static CreateWorkoutTemplateHandler NewSut(out Mock<IWorkoutTemplateRepository> templateRepository, out Mock<IExerciseRepository> exerciseRepository)
    {
        templateRepository = new Mock<IWorkoutTemplateRepository>();
        templateRepository.Setup(x => x.AddAsync(It.IsAny<WorkoutTemplateDocument>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        exerciseRepository = new Mock<IExerciseRepository>();
        exerciseRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => new Exercise { Id = id, Name = $"Exercise {id}", NamePt = $"Exercicio {id}" });

        return new CreateWorkoutTemplateHandler(templateRepository.Object, exerciseRepository.Object, new CreateWorkoutTemplateCommandValidator());
    }

    private static CreateWorkoutTemplateCommand ValidCommandWith(BlockDto block) =>
        new("Push Day", null, 4, "Strength", Difficulty.Hard, [block]);
}
