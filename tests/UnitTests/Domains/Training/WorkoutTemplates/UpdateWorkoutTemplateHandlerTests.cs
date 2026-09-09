using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Documents;
using ShapeUp.Features.Training.Shared.Entities;
using ShapeUp.Features.Training.Shared.Enums;
using ShapeUp.Features.Training.WorkoutTemplates.UpdateWorkoutTemplate;
using ShapeUp.Features.Training.Workouts.Shared.Dtos;
using ShapeUp.Features.Training.Workouts.Shared.ValueObjects;

namespace UnitTests.Domains.Training.WorkoutTemplates;

public class UpdateWorkoutTemplateHandlerTests
{
    private static WorkoutSetValueObject StraightSet(int reps = 10, int rest = 90, IntensityDto? intensity = null) =>
        new(reps, 20, LoadUnit.Kg, SetType.Working, Technique.Straight, intensity ?? new IntensityDto(IntensityType.Rpe, 8), rest);

    [Fact]
    public async Task HandleAsync_WhenActorIsNotOwner_ReturnsForbidden()
    {
        var sut = NewSut(out var templateRepository, out _, templateCreatedByUserId: 99);
        templateRepository.Setup(x => x.GetByIdAsync("template-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkoutTemplateDocument { Id = "template-1", CreatedByUserId = 99 });

        var command = ValidCommandWith(new BlockDto(BlockType.Straight, [new WorkoutExerciseDto(1, [StraightSet()])]));
        command.SetTemplateId("template-1");

        var result = await sut.HandleAsync(command, 10, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(403, result.Error!.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_WhenValid_UpdatesWorkoutTemplate()
    {
        var sut = NewSut(out var templateRepository, out _, templateCreatedByUserId: 10);
        WorkoutTemplateDocument? capturedTemplate = null;
        templateRepository.Setup(x => x.UpdateAsync(It.IsAny<WorkoutTemplateDocument>(), It.IsAny<CancellationToken>()))
            .Callback<WorkoutTemplateDocument, CancellationToken>((template, _) => capturedTemplate = template)
            .Returns(Task.CompletedTask);

        var command = ValidCommandWith(new BlockDto(BlockType.Straight, [new WorkoutExerciseDto(1, [StraightSet()])]));
        command.SetTemplateId("template-1");

        var result = await sut.HandleAsync(command, 10, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(BlockType.Straight, capturedTemplate!.Blocks[0].Type);
    }

    [Fact]
    public async Task HandleAsync_WhenSupersetHasOnlyOneExercise_ReturnsValidationError()
    {
        var sut = NewSut(out _, out _, templateCreatedByUserId: 10);
        var command = ValidCommandWith(new BlockDto(BlockType.Superset, [new WorkoutExerciseDto(1, [StraightSet()])]));
        command.SetTemplateId("template-1");

        var result = await sut.HandleAsync(command, 10, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(400, result.Error!.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_WhenAmrapMissingTimeCap_ReturnsValidationError()
    {
        var sut = NewSut(out _, out _, templateCreatedByUserId: 10);
        var setNoRest = new WorkoutSetValueObject(10, 20, LoadUnit.Kg, SetType.Working, Technique.Straight, null, null);
        var command = ValidCommandWith(new BlockDto(BlockType.Amrap, [new WorkoutExerciseDto(1, [setNoRest])]));
        command.SetTemplateId("template-1");

        var result = await sut.HandleAsync(command, 10, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(400, result.Error!.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_WhenEmomMissingIntervalOrRounds_ReturnsValidationError()
    {
        var sut = NewSut(out _, out _, templateCreatedByUserId: 10);
        var setNoRest = new WorkoutSetValueObject(10, 20, LoadUnit.Kg, SetType.Working, Technique.Straight, null, null);
        var command = ValidCommandWith(new BlockDto(BlockType.Emom, [new WorkoutExerciseDto(1, [setNoRest])]));
        command.SetTemplateId("template-1");

        var result = await sut.HandleAsync(command, 10, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(400, result.Error!.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_WhenRestSecondsSetOnNonStraightBlock_ReturnsValidationError()
    {
        var sut = NewSut(out _, out _, templateCreatedByUserId: 10);
        var command = ValidCommandWith(new BlockDto(BlockType.Superset, [
            new WorkoutExerciseDto(1, [StraightSet()]),
            new WorkoutExerciseDto(1, [StraightSet()])
        ]));
        command.SetTemplateId("template-1");

        var result = await sut.HandleAsync(command, 10, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(400, result.Error!.StatusCode);
    }

    private static UpdateWorkoutTemplateHandler NewSut(out Mock<IWorkoutTemplateRepository> templateRepository, out Mock<IExerciseRepository> exerciseRepository, int templateCreatedByUserId)
    {
        templateRepository = new Mock<IWorkoutTemplateRepository>();
        templateRepository.Setup(x => x.GetByIdAsync("template-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkoutTemplateDocument { Id = "template-1", CreatedByUserId = templateCreatedByUserId });
        templateRepository.Setup(x => x.UpdateAsync(It.IsAny<WorkoutTemplateDocument>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        exerciseRepository = new Mock<IExerciseRepository>();
        exerciseRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => new Exercise { Id = id, Name = $"Exercise {id}", NamePt = $"Exercicio {id}" });

        return new UpdateWorkoutTemplateHandler(templateRepository.Object, exerciseRepository.Object, new UpdateWorkoutTemplateCommandValidator());
    }

    private static UpdateWorkoutTemplateCommand ValidCommandWith(BlockDto block) =>
        new("Push Day", null, 4, "Strength", Difficulty.Hard, [block]);
}
