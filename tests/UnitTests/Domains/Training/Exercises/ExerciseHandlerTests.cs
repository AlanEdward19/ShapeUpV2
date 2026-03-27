namespace UnitTests.Domains.Training.Exercises;

using ShapeUp.Features.Training.Exercises.CreateExercise;
using ShapeUp.Features.Training.Exercises.GetExercises;
using ShapeUp.Features.Training.Exercises.Shared.Dtos;
using ShapeUp.Features.Training.Exercises.UpdateExercise;
using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Entities;

public class ExerciseHandlerTests
{
    private readonly Mock<IExerciseRepository> _exerciseRepository = new();
    private readonly Mock<IEquipmentRepository> _equipmentRepository = new();
    private readonly Mock<IMuscleRepository> _muscleRepository = new();

    [Fact]
    public async Task CreateExerciseHandler_ValidCommand_PersistsExercise()
    {
        _equipmentRepository.Setup(x => x.GetByIdAsync(7, default))
            .ReturnsAsync(new Equipment { Id = 7, Name = "Barbell", NamePt = "Barra" });
        _muscleRepository.Setup(x => x.GetByIdAsync(11, default))
            .ReturnsAsync(new Muscle { Id = 11, Name = "Chest", NamePt = "Peito" });
        _exerciseRepository.Setup(x => x.AddAsync(It.IsAny<Exercise>(), default))
            .Callback<Exercise, CancellationToken>((exercise, _) => exercise.Id = 99)
            .Returns(Task.CompletedTask);
        _exerciseRepository.Setup(x => x.GetByIdAsync(99, default))
            .ReturnsAsync(new Exercise
            {
                Id = 99,
                Name = "Bench Press",
                NamePt = "Supino",
                Description = "desc",
                VideoUrl = "https://video",
                MuscleProfiles =
                [
                    new ExerciseMuscleProfile
                    {
                        ExerciseId = 99,
                        MuscleId = 11,
                        ActivationPercent = 70,
                        Muscle = new Muscle { Id = 11, Name = "Chest", NamePt = "Peito" }
                    }
                ],
                ExerciseEquipments =
                [
                    new ExerciseEquipment
                    {
                        ExerciseId = 99,
                        EquipmentId = 7,
                        Equipment = new Equipment { Id = 7, Name = "Barbell", NamePt = "Barra" }
                    }
                ],
                Steps = [new ExerciseStep { ExerciseId = 99, Description = "Step 1" }]
            });

        var handler = new CreateExerciseHandler(
            _exerciseRepository.Object,
            _equipmentRepository.Object,
            _muscleRepository.Object,
            new CreateExerciseCommandValidator());

        var result = await handler.HandleAsync(
            new CreateExerciseCommand(
                "Bench Press",
                "Supino",
                "desc",
                "https://video",
                [new ExerciseMuscleDto(11, 70)],
                [7],
                [new ExerciseStepDto("Step 1")]),
            default);

        Assert.True(result.IsSuccess);
        Assert.Equal("Bench Press", result.Value!.Name);
        Assert.Equal("Supino", result.Value.NamePt);
        Assert.Single(result.Value.Muscles);
        Assert.Single(result.Value.Equipments);
        _exerciseRepository.Verify(x => x.AddAsync(It.Is<Exercise>(e => e.Name == "Bench Press" && e.NamePt == "Supino"), default), Times.Once);
    }

    [Fact]
    public async Task CreateExerciseHandler_UnknownEquipment_ReturnsNotFound()
    {
        _equipmentRepository.Setup(x => x.GetByIdAsync(7, default)).ReturnsAsync((Equipment?)null);

        var handler = new CreateExerciseHandler(
            _exerciseRepository.Object,
            _equipmentRepository.Object,
            _muscleRepository.Object,
            new CreateExerciseCommandValidator());

        var result = await handler.HandleAsync(
            new CreateExerciseCommand("Bench Press", "Supino", null, null, [new ExerciseMuscleDto(11, 70)], [7], null),
            default);

        Assert.True(result.IsFailure);
        Assert.Equal("not_found", result.Error!.Code);
    }

    [Fact]
    public async Task CreateExerciseHandler_UnknownMuscle_ReturnsNotFound()
    {
        _equipmentRepository.Setup(x => x.GetByIdAsync(7, default))
            .ReturnsAsync(new Equipment { Id = 7, Name = "Barbell", NamePt = "Barra" });
        _muscleRepository.Setup(x => x.GetByIdAsync(11, default)).ReturnsAsync((Muscle?)null);

        var handler = new CreateExerciseHandler(
            _exerciseRepository.Object,
            _equipmentRepository.Object,
            _muscleRepository.Object,
            new CreateExerciseCommandValidator());

        var result = await handler.HandleAsync(
            new CreateExerciseCommand("Bench Press", "Supino", null, null, [new ExerciseMuscleDto(11, 70)], [7], null),
            default);

        Assert.True(result.IsFailure);
        Assert.Equal("not_found", result.Error!.Code);
    }

    [Fact]
    public async Task UpdateExerciseHandler_ExerciseNotFound_ReturnsNotFound()
    {
        _exerciseRepository.Setup(x => x.GetByIdAsync(5, default)).ReturnsAsync((Exercise?)null);

        var handler = new UpdateExerciseHandler(
            _exerciseRepository.Object,
            _equipmentRepository.Object,
            _muscleRepository.Object,
            new UpdateExerciseCommandValidator());

        var result = await handler.HandleAsync(
            new UpdateExerciseCommand(5, "Bench Press", "Supino", null, null, [new ExerciseMuscleDto(11, 70)], [7], null),
            default);

        Assert.True(result.IsFailure);
        Assert.Equal("not_found", result.Error!.Code);
    }

    [Fact]
    public async Task UpdateExerciseHandler_ValidCommand_RewritesGraph()
    {
        var exercise = new Exercise
        {
            Id = 5,
            Name = "Old",
            NamePt = "Antigo",
            MuscleProfiles = [new ExerciseMuscleProfile { ExerciseId = 5, MuscleId = 1, ActivationPercent = 50 }],
            ExerciseEquipments = [new ExerciseEquipment { ExerciseId = 5, EquipmentId = 1 }],
            Steps = [new ExerciseStep { ExerciseId = 5, Description = "Old step" }]
        };

        _exerciseRepository.SetupSequence(x => x.GetByIdAsync(5, default))
            .ReturnsAsync(exercise)
            .ReturnsAsync(new Exercise
            {
                Id = 5,
                Name = "New",
                NamePt = "Novo",
                MuscleProfiles =
                [
                    new ExerciseMuscleProfile
                    {
                        ExerciseId = 5,
                        MuscleId = 11,
                        ActivationPercent = 85,
                        Muscle = new Muscle { Id = 11, Name = "Chest", NamePt = "Peito" }
                    }
                ],
                ExerciseEquipments =
                [
                    new ExerciseEquipment
                    {
                        ExerciseId = 5,
                        EquipmentId = 7,
                        Equipment = new Equipment { Id = 7, Name = "Barbell", NamePt = "Barra" }
                    }
                ],
                Steps = [new ExerciseStep { ExerciseId = 5, Description = "New step" }]
            });
        _equipmentRepository.Setup(x => x.GetByIdAsync(7, default))
            .ReturnsAsync(new Equipment { Id = 7, Name = "Barbell", NamePt = "Barra" });
        _muscleRepository.Setup(x => x.GetByIdAsync(11, default))
            .ReturnsAsync(new Muscle { Id = 11, Name = "Chest", NamePt = "Peito" });
        _exerciseRepository.Setup(x => x.UpdateAsync(It.IsAny<Exercise>(), default)).Returns(Task.CompletedTask);

        var handler = new UpdateExerciseHandler(
            _exerciseRepository.Object,
            _equipmentRepository.Object,
            _muscleRepository.Object,
            new UpdateExerciseCommandValidator());

        var result = await handler.HandleAsync(
            new UpdateExerciseCommand(5, "New", "Novo", "desc", null, [new ExerciseMuscleDto(11, 85)], [7], [new ExerciseStepDto("New step")]),
            default);

        Assert.True(result.IsSuccess);
        Assert.Equal("New", exercise.Name);
        Assert.Equal("Novo", exercise.NamePt);
        Assert.Single(exercise.MuscleProfiles);
        Assert.Equal(11, exercise.MuscleProfiles.Single().MuscleId);
        Assert.Single(exercise.ExerciseEquipments);
        Assert.Equal(7, exercise.ExerciseEquipments.Single().EquipmentId);
        Assert.Single(exercise.Steps);
        Assert.Equal("New step", exercise.Steps.Single().Description);
    }

    [Fact]
    public async Task GetExercisesHandler_InvalidCursor_ReturnsValidationFailure()
    {
        var handler = new GetExercisesHandler(_exerciseRepository.Object);

        var result = await handler.HandleAsync(new GetExercisesQuery("not-base64", 10), default);

        Assert.True(result.IsFailure);
        Assert.Equal("validation_error", result.Error!.Code);
    }
}


