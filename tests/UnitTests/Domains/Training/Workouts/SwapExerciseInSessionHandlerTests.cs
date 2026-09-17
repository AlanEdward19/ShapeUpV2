using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Documents;
using ShapeUp.Features.Training.Shared.Documents.ValueObjects;
using ShapeUp.Features.Training.Shared.Entities;
using ShapeUp.Features.Training.Shared.Enums;
using ShapeUp.Features.Training.Workouts.Shared;
using ShapeUp.Features.Training.Workouts.Shared.Dtos;
using ShapeUp.Features.Training.Workouts.Shared.ValueObjects;
using ShapeUp.Features.Training.Workouts.SwapExerciseInSession;

namespace UnitTests.Domains.Training.Workouts;

public class SwapExerciseInSessionHandlerTests
{
    private static WorkoutSetValueObject RetainedSet(int reps = 10) =>
        new(reps, 30, LoadUnit.Kg, SetType.Working, Technique.Straight, new IntensityDto(IntensityType.Rpe, 8), 90);

    [Fact]
    public async Task HandleAsync_WhenValid_RetainsSetsAndAppendsNewExercise()
    {
        var session = ActiveSessionWithExercise(1, setsCompleted: 2);
        var sessionRepository = new Mock<IWorkoutSessionRepository>();
        sessionRepository.Setup(x => x.GetByIdAsync("s1", It.IsAny<CancellationToken>())).ReturnsAsync(session);
        List<ExecutedExerciseDocumentValueObject>? persisted = null;
        sessionRepository
            .Setup(x => x.UpdateStateAsync("s1", It.IsAny<DateTime>(), It.IsAny<List<ExecutedExerciseDocumentValueObject>>(), It.IsAny<CancellationToken>()))
            .Callback<string, DateTime, List<ExecutedExerciseDocumentValueObject>, CancellationToken>((_, _, exercises, _) => persisted = exercises)
            .Returns(Task.CompletedTask);

        var exerciseRepository = new Mock<IExerciseRepository>();
        exerciseRepository
            .Setup(x => x.GetByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exercise { Id = 2, Name = "Push Up", NamePt = "Flexao" });

        var equivalentRepository = new Mock<IExerciseEquivalentRepository>();
        equivalentRepository
            .Setup(x => x.GetEquivalentsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Exercise { Id = 2, Name = "Push Up", NamePt = "Flexao" }]);

        var sut = new SwapExerciseInSessionHandler(
            sessionRepository.Object,
            exerciseRepository.Object,
            equivalentRepository.Object,
            new WorkoutSessionResponseMapper(),
            new SwapExerciseInSessionCommandValidator());

        var result = await sut.HandleAsync(
            new SwapExerciseInSessionCommand("s1", 1, 2, [RetainedSet(), RetainedSet(8)]),
            10,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(persisted);
        Assert.Equal(2, persisted!.Count);
        Assert.Equal(1, persisted[0].ExerciseId);
        Assert.Equal(2, persisted[0].Sets.Count);
        Assert.Equal(2, persisted[1].ExerciseId);
        Assert.Empty(persisted[1].Sets);
        Assert.False(persisted[1].RequireRpe);
    }

    [Fact]
    public async Task HandleAsync_WhenNotEquivalent_ReturnsValidationError()
    {
        var session = ActiveSessionWithExercise(1);
        var sessionRepository = new Mock<IWorkoutSessionRepository>();
        sessionRepository.Setup(x => x.GetByIdAsync("s1", It.IsAny<CancellationToken>())).ReturnsAsync(session);

        var equivalentRepository = new Mock<IExerciseEquivalentRepository>();
        equivalentRepository
            .Setup(x => x.GetEquivalentsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var sut = new SwapExerciseInSessionHandler(
            sessionRepository.Object,
            new Mock<IExerciseRepository>().Object,
            equivalentRepository.Object,
            new WorkoutSessionResponseMapper(),
            new SwapExerciseInSessionCommandValidator());

        var result = await sut.HandleAsync(
            new SwapExerciseInSessionCommand("s1", 1, 2, [RetainedSet()]),
            10,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(400, result.Error!.StatusCode);
        sessionRepository.Verify(
            x => x.UpdateStateAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<List<ExecutedExerciseDocumentValueObject>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenNewExerciseAlreadyInSession_ReturnsValidationError()
    {
        var session = ActiveSessionWithExercise(1);
        session.Exercises.Add(new ExecutedExerciseDocumentValueObject { ExerciseId = 2, ExerciseName = "Push Up" });
        var sessionRepository = new Mock<IWorkoutSessionRepository>();
        sessionRepository.Setup(x => x.GetByIdAsync("s1", It.IsAny<CancellationToken>())).ReturnsAsync(session);

        var equivalentRepository = new Mock<IExerciseEquivalentRepository>();
        equivalentRepository
            .Setup(x => x.GetEquivalentsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Exercise { Id = 2, Name = "Push Up", NamePt = "Flexao" }]);

        var sut = new SwapExerciseInSessionHandler(
            sessionRepository.Object,
            new Mock<IExerciseRepository>().Object,
            equivalentRepository.Object,
            new WorkoutSessionResponseMapper(),
            new SwapExerciseInSessionCommandValidator());

        var result = await sut.HandleAsync(
            new SwapExerciseInSessionCommand("s1", 1, 2, []),
            10,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(400, result.Error!.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_WhenSessionCompleted_ReturnsConflict()
    {
        var session = ActiveSessionWithExercise(1);
        session.IsCompleted = true;
        var sessionRepository = new Mock<IWorkoutSessionRepository>();
        sessionRepository.Setup(x => x.GetByIdAsync("s1", It.IsAny<CancellationToken>())).ReturnsAsync(session);

        var sut = new SwapExerciseInSessionHandler(
            sessionRepository.Object,
            new Mock<IExerciseRepository>().Object,
            new Mock<IExerciseEquivalentRepository>().Object,
            new WorkoutSessionResponseMapper(),
            new SwapExerciseInSessionCommandValidator());

        var result = await sut.HandleAsync(
            new SwapExerciseInSessionCommand("s1", 1, 2, []),
            10,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(409, result.Error!.StatusCode);
    }

    private static WorkoutSessionDocument ActiveSessionWithExercise(int exerciseId, int setsCompleted = 0) =>
        new()
        {
            Id = "s1",
            TargetUserId = 10,
            ExecutedByUserId = 10,
            IsCompleted = false,
            IsCancelled = false,
            Exercises =
            [
                new ExecutedExerciseDocumentValueObject
                {
                    ExerciseId = exerciseId,
                    ExerciseName = "Bench",
                    Sets = Enumerable.Range(0, setsCompleted)
                        .Select(_ => new ExecutedSetDocumentValueObject
                        {
                            Repetitions = 10,
                            Load = 30,
                            LoadUnit = LoadUnit.Kg,
                            SetType = SetType.Working,
                            Technique = Technique.Straight,
                            RestSeconds = 90
                        })
                        .ToList()
                }
            ]
        };
}
