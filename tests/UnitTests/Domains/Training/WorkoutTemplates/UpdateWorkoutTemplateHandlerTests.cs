using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Documents;
using ShapeUp.Features.Training.Shared.Documents.ValueObjects;
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

    // --- workout-execution-validation: RequireRpe persistence (WEV-05) ---

    [Fact]
    public async Task HandleAsync_WhenExerciseFlipsRequireRpeFromFalseToTrue_PersistsAndReturnsTrue()
    {
        var sut = NewSut(out var templateRepository, out _, templateCreatedByUserId: 10);
        WorkoutTemplateDocument? capturedTemplate = null;
        templateRepository.Setup(x => x.UpdateAsync(It.IsAny<WorkoutTemplateDocument>(), It.IsAny<CancellationToken>()))
            .Callback<WorkoutTemplateDocument, CancellationToken>((template, _) => capturedTemplate = template)
            .Returns(Task.CompletedTask);

        var command = ValidCommandWith(new BlockDto(BlockType.Straight, [new WorkoutExerciseDto(1, [StraightSet()], RequireRpe: true)]));
        command.SetTemplateId("template-1");

        var result = await sut.HandleAsync(command, 10, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(capturedTemplate!.Blocks[0].Exercises[0].RequireRpe);
        Assert.True(result.Value!.Blocks[0].Exercises[0].RequireRpe);
    }

    [Fact]
    public async Task HandleAsync_WhenExerciseFlipsRequireRpeFromTrueToFalse_PersistsAndReturnsFalse()
    {
        var sut = NewSut(out var templateRepository, out _, templateCreatedByUserId: 10);
        templateRepository.Setup(x => x.GetByIdAsync("template-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkoutTemplateDocument
            {
                Id = "template-1",
                CreatedByUserId = 10,
                Blocks = [new BlockDocumentValueObject
                {
                    Type = BlockType.Straight,
                    Exercises = [new BlockExerciseDocumentValueObject { ExerciseId = 1, ExerciseName = "Exercise 1", RequireRpe = true }]
                }]
            });
        WorkoutTemplateDocument? capturedTemplate = null;
        templateRepository.Setup(x => x.UpdateAsync(It.IsAny<WorkoutTemplateDocument>(), It.IsAny<CancellationToken>()))
            .Callback<WorkoutTemplateDocument, CancellationToken>((template, _) => capturedTemplate = template)
            .Returns(Task.CompletedTask);

        var command = ValidCommandWith(new BlockDto(BlockType.Straight, [new WorkoutExerciseDto(1, [StraightSet()], RequireRpe: false)]));
        command.SetTemplateId("template-1");

        var result = await sut.HandleAsync(command, 10, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(capturedTemplate!.Blocks[0].Exercises[0].RequireRpe);
        Assert.False(result.Value!.Blocks[0].Exercises[0].RequireRpe);
    }

    // --- time-based-exercises: TBE-02 TimeBased gate (T18, mirrors T15/T17) ---

    [Fact]
    public async Task HandleAsync_WhenTimeBasedExerciseSetMissingDuration_ReturnsValidationErrorNamingExercise()
    {
        var templateRepository = new Mock<IWorkoutTemplateRepository>();
        templateRepository.Setup(x => x.GetByIdAsync("template-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkoutTemplateDocument { Id = "template-1", CreatedByUserId = 10 });

        var exerciseRepository = new Mock<IExerciseRepository>();
        exerciseRepository
            .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exercise { Id = 1, Name = "Running", NamePt = "Corrida", ExerciseType = ExerciseType.TimeBased });

        var sut = new UpdateWorkoutTemplateHandler(templateRepository.Object, exerciseRepository.Object, new UpdateWorkoutTemplateCommandValidator());

        var timeBasedSetMissingDuration = new WorkoutSetValueObject(null, null, LoadUnit.Kg, SetType.Working, Technique.Straight, null, null);
        var command = ValidCommandWith(new BlockDto(BlockType.Straight, [new WorkoutExerciseDto(1, [timeBasedSetMissingDuration])]));
        command.SetTemplateId("template-1");

        var result = await sut.HandleAsync(command, 10, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(400, result.Error!.StatusCode);
        Assert.Contains("'1'", result.Error.Message, StringComparison.Ordinal);
        templateRepository.Verify(x => x.UpdateAsync(It.IsAny<WorkoutTemplateDocument>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenTimeBasedExerciseSetHasDurationWithoutDistance_UpdatesWorkoutTemplate()
    {
        var templateRepository = new Mock<IWorkoutTemplateRepository>();
        templateRepository.Setup(x => x.GetByIdAsync("template-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkoutTemplateDocument { Id = "template-1", CreatedByUserId = 10 });
        WorkoutTemplateDocument? capturedTemplate = null;
        templateRepository.Setup(x => x.UpdateAsync(It.IsAny<WorkoutTemplateDocument>(), It.IsAny<CancellationToken>()))
            .Callback<WorkoutTemplateDocument, CancellationToken>((template, _) => capturedTemplate = template)
            .Returns(Task.CompletedTask);

        var exerciseRepository = new Mock<IExerciseRepository>();
        exerciseRepository
            .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exercise { Id = 1, Name = "Stretching", NamePt = "Alongamento", ExerciseType = ExerciseType.TimeBased });

        var sut = new UpdateWorkoutTemplateHandler(templateRepository.Object, exerciseRepository.Object, new UpdateWorkoutTemplateCommandValidator());

        var timeBasedSet = new WorkoutSetValueObject(null, null, LoadUnit.Kg, SetType.Working, Technique.Straight, null, null, DurationSeconds: 120);
        var command = ValidCommandWith(new BlockDto(BlockType.Straight, [new WorkoutExerciseDto(1, [timeBasedSet])]));
        command.SetTemplateId("template-1");

        var result = await sut.HandleAsync(command, 10, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(120, capturedTemplate!.Blocks[0].Exercises[0].Sets[0].DurationSeconds);
        Assert.Null(capturedTemplate.Blocks[0].Exercises[0].Sets[0].DistanceMeters);
    }

    [Fact]
    public async Task HandleAsync_WhenTimeBasedExerciseSetHasNonStraightTechnique_ReturnsValidationError()
    {
        var templateRepository = new Mock<IWorkoutTemplateRepository>();
        templateRepository.Setup(x => x.GetByIdAsync("template-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkoutTemplateDocument { Id = "template-1", CreatedByUserId = 10 });

        var exerciseRepository = new Mock<IExerciseRepository>();
        exerciseRepository
            .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exercise { Id = 1, Name = "Running", NamePt = "Corrida", ExerciseType = ExerciseType.TimeBased });

        var sut = new UpdateWorkoutTemplateHandler(templateRepository.Object, exerciseRepository.Object, new UpdateWorkoutTemplateCommandValidator());

        var timeBasedSetWithDropSet = new WorkoutSetValueObject(null, null, LoadUnit.Kg, SetType.Working, Technique.DropSet, null, null, DurationSeconds: 120);
        var command = ValidCommandWith(new BlockDto(BlockType.Straight, [new WorkoutExerciseDto(1, [timeBasedSetWithDropSet])]));
        command.SetTemplateId("template-1");

        var result = await sut.HandleAsync(command, 10, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(400, result.Error!.StatusCode);
        templateRepository.Verify(x => x.UpdateAsync(It.IsAny<WorkoutTemplateDocument>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenWeightBasedExerciseSetHasNoDuration_UpdatesWorkoutTemplateUnaffectedByTimeBasedGate()
    {
        // Regression: the new TimeBased gate (DurationSeconds/Technique) must never engage for a
        // WeightBased exercise -- WEV-01/02 behavior stays exactly as it was before this feature.
        var sut = NewSut(out var templateRepository, out _, templateCreatedByUserId: 10);
        WorkoutTemplateDocument? capturedTemplate = null;
        templateRepository.Setup(x => x.UpdateAsync(It.IsAny<WorkoutTemplateDocument>(), It.IsAny<CancellationToken>()))
            .Callback<WorkoutTemplateDocument, CancellationToken>((template, _) => capturedTemplate = template)
            .Returns(Task.CompletedTask);

        var command = ValidCommandWith(new BlockDto(BlockType.Straight, [new WorkoutExerciseDto(1, [StraightSet()])]));
        command.SetTemplateId("template-1");

        var result = await sut.HandleAsync(command, 10, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(capturedTemplate!.Blocks[0].Exercises[0].Sets[0].DurationSeconds);
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
