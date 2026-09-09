using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Documents;
using ShapeUp.Features.Training.Shared.Entities;
using ShapeUp.Features.Training.Shared.Enums;
using ShapeUp.Features.Training.WorkoutPlans.UpdateWorkoutPlan;
using ShapeUp.Features.Training.Workouts.Shared.Dtos;
using ShapeUp.Features.Training.Workouts.Shared.ValueObjects;

namespace UnitTests.Domains.Training.WorkoutPlans;

public class UpdateWorkoutPlanHandlerTests
{
    private static WorkoutSetValueObject StraightSet(int reps = 10, int rest = 90, IntensityDto? intensity = null) =>
        new(reps, 20, LoadUnit.Kg, SetType.Working, Technique.Straight, intensity ?? new IntensityDto(IntensityType.Rpe, 8), rest);

    [Fact]
    public async Task HandleAsync_WhenActorIsNotOwner_ReturnsForbidden()
    {
        var sut = NewSut(out var planRepository, out _, planCreatedByUserId: 99);
        planRepository.Setup(x => x.GetByIdAsync("plan-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkoutPlanDocument { Id = "plan-1", CreatedByUserId = 99 });

        var command = ValidCommandWith(new BlockDto(BlockType.Straight, [new WorkoutExerciseDto(1, [StraightSet()])]));
        command.SetPlanId("plan-1");

        var result = await sut.HandleAsync(command, 10, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(403, result.Error!.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_WhenValid_UpdatesWorkoutPlan()
    {
        var sut = NewSut(out var planRepository, out _, planCreatedByUserId: 10);
        WorkoutPlanDocument? capturedPlan = null;
        planRepository.Setup(x => x.UpdateAsync(It.IsAny<WorkoutPlanDocument>(), It.IsAny<CancellationToken>()))
            .Callback<WorkoutPlanDocument, CancellationToken>((plan, _) => capturedPlan = plan)
            .Returns(Task.CompletedTask);

        var command = ValidCommandWith(new BlockDto(BlockType.Straight, [new WorkoutExerciseDto(1, [StraightSet()])]));
        command.SetPlanId("plan-1");

        var result = await sut.HandleAsync(command, 10, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(BlockType.Straight, capturedPlan!.Blocks[0].Type);
    }

    [Fact]
    public async Task HandleAsync_WhenSupersetHasOnlyOneExercise_ReturnsValidationError()
    {
        var sut = NewSut(out _, out _, planCreatedByUserId: 10);
        var command = ValidCommandWith(new BlockDto(BlockType.Superset, [new WorkoutExerciseDto(1, [StraightSet()])]));
        command.SetPlanId("plan-1");

        var result = await sut.HandleAsync(command, 10, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(400, result.Error!.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_WhenAmrapMissingTimeCap_ReturnsValidationError()
    {
        var sut = NewSut(out _, out _, planCreatedByUserId: 10);
        var setNoRest = new WorkoutSetValueObject(10, 20, LoadUnit.Kg, SetType.Working, Technique.Straight, null, null);
        var command = ValidCommandWith(new BlockDto(BlockType.Amrap, [new WorkoutExerciseDto(1, [setNoRest])]));
        command.SetPlanId("plan-1");

        var result = await sut.HandleAsync(command, 10, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(400, result.Error!.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_WhenEmomMissingIntervalOrRounds_ReturnsValidationError()
    {
        var sut = NewSut(out _, out _, planCreatedByUserId: 10);
        var setNoRest = new WorkoutSetValueObject(10, 20, LoadUnit.Kg, SetType.Working, Technique.Straight, null, null);
        var command = ValidCommandWith(new BlockDto(BlockType.Emom, [new WorkoutExerciseDto(1, [setNoRest])]));
        command.SetPlanId("plan-1");

        var result = await sut.HandleAsync(command, 10, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(400, result.Error!.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_WhenRestSecondsSetOnNonStraightBlock_ReturnsValidationError()
    {
        var sut = NewSut(out _, out _, planCreatedByUserId: 10);
        var command = ValidCommandWith(new BlockDto(BlockType.Superset, [
            new WorkoutExerciseDto(1, [StraightSet()]), // has RestSeconds = 90
            new WorkoutExerciseDto(1, [StraightSet()])
        ]));
        command.SetPlanId("plan-1");

        var result = await sut.HandleAsync(command, 10, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(400, result.Error!.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_WhenValidAmrapAndEmomBlocks_UpdatesWorkoutPlan()
    {
        var sut = NewSut(out var planRepository, out _, planCreatedByUserId: 10);
        WorkoutPlanDocument? capturedPlan = null;
        planRepository.Setup(x => x.UpdateAsync(It.IsAny<WorkoutPlanDocument>(), It.IsAny<CancellationToken>()))
            .Callback<WorkoutPlanDocument, CancellationToken>((plan, _) => capturedPlan = plan)
            .Returns(Task.CompletedTask);

        var noRestSet = new WorkoutSetValueObject(null, 20, LoadUnit.Kg, SetType.Working, Technique.Straight, null, null);
        var command = new UpdateWorkoutPlanCommand(
            "Push Day",
            null,
            4,
            "Strength",
            Difficulty.Hard,
            [
                new BlockDto(BlockType.Amrap, [new WorkoutExerciseDto(1, [noRestSet])], TimeCapSeconds: 600),
                new BlockDto(BlockType.Emom, [new WorkoutExerciseDto(1, [noRestSet]), new WorkoutExerciseDto(1, [noRestSet])], IntervalSeconds: 60, TotalRounds: 10)
            ]);
        command.SetPlanId("plan-1");

        var result = await sut.HandleAsync(command, 10, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(600, capturedPlan!.Blocks[0].TimeCapSeconds);
        Assert.Equal(60, capturedPlan.Blocks[1].IntervalSeconds);
        Assert.Equal(10, capturedPlan.Blocks[1].TotalRounds);
    }

    private static UpdateWorkoutPlanHandler NewSut(out Mock<IWorkoutPlanRepository> planRepository, out Mock<IExerciseRepository> exerciseRepository, int planCreatedByUserId)
    {
        planRepository = new Mock<IWorkoutPlanRepository>();
        planRepository.Setup(x => x.GetByIdAsync("plan-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkoutPlanDocument { Id = "plan-1", CreatedByUserId = planCreatedByUserId });
        planRepository.Setup(x => x.UpdateAsync(It.IsAny<WorkoutPlanDocument>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        exerciseRepository = new Mock<IExerciseRepository>();
        exerciseRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => new Exercise { Id = id, Name = $"Exercise {id}", NamePt = $"Exercicio {id}" });

        return new UpdateWorkoutPlanHandler(planRepository.Object, exerciseRepository.Object, new UpdateWorkoutPlanCommandValidator());
    }

    private static UpdateWorkoutPlanCommand ValidCommandWith(BlockDto block) =>
        new("Push Day", null, 4, "Strength", Difficulty.Hard, [block]);
}
