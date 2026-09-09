using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Documents;
using ShapeUp.Features.Training.Shared.Entities;
using ShapeUp.Features.Training.Shared.Enums;
using ShapeUp.Features.Training.WorkoutPlans.CreateWorkoutPlan;
using ShapeUp.Features.Training.Workouts.Shared.Dtos;
using ShapeUp.Features.Training.Workouts.Shared.ValueObjects;

namespace UnitTests.Domains.Training.WorkoutPlans;

public class CreateWorkoutPlanHandlerTests
{
    private static WorkoutSetValueObject StraightSet(int reps = 10, int rest = 90, IntensityDto? intensity = null) =>
        new(reps, 20, LoadUnit.Kg, SetType.Working, Technique.Straight, intensity ?? new IntensityDto(IntensityType.Rpe, 8), rest);

    [Fact]
    public async Task HandleAsync_WhenActorCannotCreateForTarget_ReturnsForbidden()
    {
        var planRepository = new Mock<IWorkoutPlanRepository>();
        var exerciseRepository = new Mock<IExerciseRepository>();
        var accessPolicy = new Mock<ITrainingAccessPolicy>();
        accessPolicy
            .Setup(x => x.CanCreateWorkoutForAsync(11, 22, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var sut = new CreateWorkoutPlanHandler(planRepository.Object, exerciseRepository.Object, accessPolicy.Object, new CreateWorkoutPlanCommandValidator());

        var command = new CreateWorkoutPlanCommand(
            22,
            "Plan A",
            null,
            4,
            "Hypertrophy",
            Difficulty.Intermediate,
            [new BlockDto(BlockType.Straight, [new WorkoutExerciseDto(1, [StraightSet()])])]);

        var result = await sut.HandleAsync(command, 11, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(403, result.Error!.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_WhenValid_CreatesWorkoutPlan()
    {
        var planRepository = new Mock<IWorkoutPlanRepository>();
        WorkoutPlanDocument? capturedPlan = null;
        planRepository
            .Setup(x => x.AddAsync(It.IsAny<WorkoutPlanDocument>(), It.IsAny<CancellationToken>()))
            .Callback<WorkoutPlanDocument, CancellationToken>((plan, _) =>
            {
                plan.Id = "plan-1";
                capturedPlan = plan;
            })
            .Returns(Task.CompletedTask);

        var exerciseRepository = new Mock<IExerciseRepository>();
        exerciseRepository
            .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exercise { Id = 1, Name = "Bench Press", NamePt = "Supino" });

        var accessPolicy = new Mock<ITrainingAccessPolicy>();
        accessPolicy
            .Setup(x => x.CanCreateWorkoutForAsync(10, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = new CreateWorkoutPlanHandler(planRepository.Object, exerciseRepository.Object, accessPolicy.Object, new CreateWorkoutPlanCommandValidator());

        var command = new CreateWorkoutPlanCommand(
            10,
            " Push Day ",
            " notes ",
            4,
            " Strength ",
            Difficulty.Hard,
            [new BlockDto(BlockType.Straight, [new WorkoutExerciseDto(1, [StraightSet(8, 120)])])]);

        var result = await sut.HandleAsync(command, 10, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(capturedPlan);
        Assert.Equal("Push Day", capturedPlan!.Name);
        Assert.Equal("notes", capturedPlan.Notes);
        Assert.Equal("plan-1", result.Value!.PlanId);
    }

    [Fact]
    public async Task HandleAsync_WhenCommandHasClientSuppliedId_UsesItAsPlanId()
    {
        var planRepository = new Mock<IWorkoutPlanRepository>();
        string? capturedId = null;
        planRepository
            .Setup(x => x.AddAsync(It.IsAny<WorkoutPlanDocument>(), It.IsAny<CancellationToken>()))
            .Callback<WorkoutPlanDocument, CancellationToken>((plan, _) => capturedId = plan.Id)
            .Returns(Task.CompletedTask);

        var exerciseRepository = new Mock<IExerciseRepository>();
        exerciseRepository
            .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exercise { Id = 1, Name = "Bench Press", NamePt = "Supino" });

        var accessPolicy = new Mock<ITrainingAccessPolicy>();
        accessPolicy
            .Setup(x => x.CanCreateWorkoutForAsync(10, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = new CreateWorkoutPlanHandler(planRepository.Object, exerciseRepository.Object, accessPolicy.Object, new CreateWorkoutPlanCommandValidator());

        const string clientSuppliedId = "507f1f77bcf86cd799439011";
        var command = new CreateWorkoutPlanCommand(
            10,
            "Push Day",
            null,
            4,
            "Strength",
            Difficulty.Hard,
            [new BlockDto(BlockType.Straight, [new WorkoutExerciseDto(1, [StraightSet(8, 120)])])],
            clientSuppliedId);

        var result = await sut.HandleAsync(command, 10, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(clientSuppliedId, capturedId);
        Assert.Equal(clientSuppliedId, result.Value!.PlanId);
    }

    [Fact]
    public async Task HandleAsync_WhenIdIsNotValidObjectIdFormat_ReturnsValidationError()
    {
        var planRepository = new Mock<IWorkoutPlanRepository>();
        var exerciseRepository = new Mock<IExerciseRepository>();
        var accessPolicy = new Mock<ITrainingAccessPolicy>();
        var sut = new CreateWorkoutPlanHandler(planRepository.Object, exerciseRepository.Object, accessPolicy.Object, new CreateWorkoutPlanCommandValidator());

        var command = new CreateWorkoutPlanCommand(
            10,
            "Push Day",
            null,
            4,
            "Strength",
            Difficulty.Hard,
            [new BlockDto(BlockType.Straight, [new WorkoutExerciseDto(1, [StraightSet(8, 120)])])],
            "not-a-valid-object-id");

        var result = await sut.HandleAsync(command, 10, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(400, result.Error!.StatusCode);
    }

    // --- workout-editor: Block validation (WOED-01..08) ---

    [Fact]
    public async Task HandleAsync_WhenSupersetHasOnlyOneExercise_ReturnsValidationError()
    {
        var sut = NewSut(out _, out _, out _);

        var command = ValidCommandWith(
            new BlockDto(BlockType.Superset, [new WorkoutExerciseDto(1, [StraightSet()])]));

        var result = await sut.HandleAsync(command, 10, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(400, result.Error!.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_WhenSupersetHasTwoExercises_CreatesWorkoutPlan()
    {
        var sut = NewSut(out _, out var planRepository, out _);
        WorkoutPlanDocument? capturedPlan = null;
        planRepository
            .Setup(x => x.AddAsync(It.IsAny<WorkoutPlanDocument>(), It.IsAny<CancellationToken>()))
            .Callback<WorkoutPlanDocument, CancellationToken>((plan, _) => capturedPlan = plan)
            .Returns(Task.CompletedTask);

        var command = ValidCommandWith(
            new BlockDto(BlockType.Superset, [
                new WorkoutExerciseDto(1, [StraightSet()]),
                new WorkoutExerciseDto(1, [StraightSet()])
            ]));

        var result = await sut.HandleAsync(command, 10, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(capturedPlan);
        Assert.Equal(BlockType.Superset, capturedPlan!.Blocks[0].Type);
        Assert.Equal(2, capturedPlan.Blocks[0].Exercises.Count);
    }

    [Fact]
    public async Task HandleAsync_WhenAmrapMissingTimeCap_ReturnsValidationError()
    {
        var sut = NewSut(out _, out _, out _);

        var setNoRest = new WorkoutSetValueObject(10, 20, LoadUnit.Kg, SetType.Working, Technique.Straight, null, null);
        var command = ValidCommandWith(
            new BlockDto(BlockType.Amrap, [new WorkoutExerciseDto(1, [setNoRest])]));

        var result = await sut.HandleAsync(command, 10, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(400, result.Error!.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_WhenAmrapValidWithoutFixedReps_CreatesWorkoutPlan()
    {
        var sut = NewSut(out _, out var planRepository, out _);
        WorkoutPlanDocument? capturedPlan = null;
        planRepository
            .Setup(x => x.AddAsync(It.IsAny<WorkoutPlanDocument>(), It.IsAny<CancellationToken>()))
            .Callback<WorkoutPlanDocument, CancellationToken>((plan, _) => capturedPlan = plan)
            .Returns(Task.CompletedTask);

        var noRepsSet = new WorkoutSetValueObject(null, 20, LoadUnit.Kg, SetType.Working, Technique.Straight, null, null);
        var command = ValidCommandWith(
            new BlockDto(BlockType.Amrap, [new WorkoutExerciseDto(1, [noRepsSet])], TimeCapSeconds: 600));

        var result = await sut.HandleAsync(command, 10, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(600, capturedPlan!.Blocks[0].TimeCapSeconds);
        Assert.Null(capturedPlan.Blocks[0].Exercises[0].Sets[0].Repetitions);
    }

    [Fact]
    public async Task HandleAsync_WhenEmomMissingIntervalOrRounds_ReturnsValidationError()
    {
        var sut = NewSut(out _, out _, out _);

        var setNoRest = new WorkoutSetValueObject(10, 20, LoadUnit.Kg, SetType.Working, Technique.Straight, null, null);
        var command = ValidCommandWith(
            new BlockDto(BlockType.Emom, [new WorkoutExerciseDto(1, [setNoRest])]));

        var result = await sut.HandleAsync(command, 10, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(400, result.Error!.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_WhenEmomValidWithTwoExerciseRotation_PreservesOrder()
    {
        var sut = NewSut(out _, out var planRepository, out _);
        WorkoutPlanDocument? capturedPlan = null;
        planRepository
            .Setup(x => x.AddAsync(It.IsAny<WorkoutPlanDocument>(), It.IsAny<CancellationToken>()))
            .Callback<WorkoutPlanDocument, CancellationToken>((plan, _) => capturedPlan = plan)
            .Returns(Task.CompletedTask);

        var setNoRest = new WorkoutSetValueObject(10, 20, LoadUnit.Kg, SetType.Working, Technique.Straight, null, null);
        var command = ValidCommandWith(
            new BlockDto(BlockType.Emom, [
                new WorkoutExerciseDto(1, [setNoRest]),
                new WorkoutExerciseDto(2, [setNoRest])
            ], IntervalSeconds: 60, TotalRounds: 10));

        var result = await sut.HandleAsync(command, 10, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(60, capturedPlan!.Blocks[0].IntervalSeconds);
        Assert.Equal(10, capturedPlan.Blocks[0].TotalRounds);
        Assert.Equal(1, capturedPlan.Blocks[0].Exercises[0].ExerciseId);
        Assert.Equal(2, capturedPlan.Blocks[0].Exercises[1].ExerciseId);
    }

    [Fact]
    public async Task HandleAsync_WhenRestSecondsSetOnNonStraightBlock_ReturnsValidationError()
    {
        var sut = NewSut(out _, out _, out _);

        var command = ValidCommandWith(
            new BlockDto(BlockType.Superset, [
                new WorkoutExerciseDto(1, [StraightSet()]), // has RestSeconds = 90
                new WorkoutExerciseDto(1, [StraightSet()])
            ]));

        var result = await sut.HandleAsync(command, 10, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(400, result.Error!.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_WhenIntensityOmitted_CreatesWorkoutPlan()
    {
        var sut = NewSut(out _, out var planRepository, out _);
        WorkoutPlanDocument? capturedPlan = null;
        planRepository
            .Setup(x => x.AddAsync(It.IsAny<WorkoutPlanDocument>(), It.IsAny<CancellationToken>()))
            .Callback<WorkoutPlanDocument, CancellationToken>((plan, _) => capturedPlan = plan)
            .Returns(Task.CompletedTask);

        var setNoIntensity = new WorkoutSetValueObject(8, 80, LoadUnit.Kg, SetType.Working, Technique.Straight, null, 90);
        var command = ValidCommandWith(
            new BlockDto(BlockType.Straight, [new WorkoutExerciseDto(1, [setNoIntensity])]));

        var result = await sut.HandleAsync(command, 10, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(capturedPlan!.Blocks[0].Exercises[0].Sets[0].Intensity);
    }

    [Fact]
    public async Task HandleAsync_WhenIntensityIsRir_CreatesWorkoutPlanWithRirIntensity()
    {
        var sut = NewSut(out _, out var planRepository, out _);
        WorkoutPlanDocument? capturedPlan = null;
        planRepository
            .Setup(x => x.AddAsync(It.IsAny<WorkoutPlanDocument>(), It.IsAny<CancellationToken>()))
            .Callback<WorkoutPlanDocument, CancellationToken>((plan, _) => capturedPlan = plan)
            .Returns(Task.CompletedTask);

        var command = ValidCommandWith(
            new BlockDto(BlockType.Straight, [new WorkoutExerciseDto(1, [StraightSet(intensity: new IntensityDto(IntensityType.Rir, 2))])]));

        var result = await sut.HandleAsync(command, 10, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var intensity = capturedPlan!.Blocks[0].Exercises[0].Sets[0].Intensity;
        Assert.NotNull(intensity);
        Assert.Equal(IntensityType.Rir, intensity!.Type);
        Assert.Equal(2, intensity.Value);
    }

    private static CreateWorkoutPlanHandler NewSut(out Mock<IExerciseRepository> exerciseRepository, out Mock<IWorkoutPlanRepository> planRepository, out Mock<ITrainingAccessPolicy> accessPolicy)
    {
        planRepository = new Mock<IWorkoutPlanRepository>();
        planRepository
            .Setup(x => x.AddAsync(It.IsAny<WorkoutPlanDocument>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        exerciseRepository = new Mock<IExerciseRepository>();
        exerciseRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exercise { Id = 1, Name = "Bench Press", NamePt = "Supino" });

        accessPolicy = new Mock<ITrainingAccessPolicy>();
        accessPolicy
            .Setup(x => x.CanCreateWorkoutForAsync(10, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        return new CreateWorkoutPlanHandler(planRepository.Object, exerciseRepository.Object, accessPolicy.Object, new CreateWorkoutPlanCommandValidator());
    }

    private static CreateWorkoutPlanCommand ValidCommandWith(BlockDto block) =>
        new(10, "Push Day", null, 4, "Strength", Difficulty.Hard, [block]);
}
