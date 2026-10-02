using MassTransit;
using ShapeUp.Configurations;
using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Documents;
using ShapeUp.Features.Training.Shared.Documents.ValueObjects;
using ShapeUp.Features.Training.Shared.Enums;
using ShapeUp.Features.Training.Workouts.FinishWorkoutExecution;

namespace UnitTests.Domains.Training.Workouts;

public class FinishWorkoutNextSessionTests
{
    private static readonly DateTime EndedAt = new(2026, 10, 1, 18, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IWorkoutSessionRepository> _repository = new();
    private WorkoutSessionDocument? _created;

    private FinishWorkoutExecutionHandler CreateHandler(WorkoutSessionDocument session, WorkoutSessionDocument? otherOpen = null)
    {
        _repository.Setup(x => x.GetByIdAsync(session.Id, It.IsAny<CancellationToken>())).ReturnsAsync(session);
        _repository.Setup(x => x.GetActiveByTargetUserIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(otherOpen ?? session);
        _repository
            .Setup(x => x.GetCompletedByUserInRangeAsync(10, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _repository
            .Setup(x => x.AddAsync(It.IsAny<WorkoutSessionDocument>(), It.IsAny<CancellationToken>(), It.IsAny<MongoDB.Driver.IClientSessionHandle>()))
            .Callback<WorkoutSessionDocument, CancellationToken, MongoDB.Driver.IClientSessionHandle>((s, _, _) => _created = s)
            .Returns(Task.CompletedTask);

        var outbox = new Mock<IWorkoutOutboxTransaction>();
        outbox
            .Setup(x => x.ExecuteAsync(It.IsAny<Func<MongoDB.Driver.IClientSessionHandle, CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<MongoDB.Driver.IClientSessionHandle, CancellationToken, Task>, CancellationToken>((action, ct) => action(null!, ct));

        return new FinishWorkoutExecutionHandler(
            _repository.Object, new FinishWorkoutExecutionCommandValidator(), new Mock<IPublishEndpoint>().Object, outbox.Object, new NoOpOutboxFaultInjector());
    }

    private static ExecutedSetDocumentValueObject Planned(decimal load, int reps) =>
        new() { Load = load, Repetitions = reps, IsPerformed = false, RestSeconds = 60 };

    private static ExecutedSetDocumentValueObject Done(decimal load, int reps, bool extra = false) =>
        new() { Load = load, Repetitions = reps, IsPerformed = true, IsExtra = extra };

    private static WorkoutSessionDocument Session(params ExecutedExerciseDocumentValueObject[] exercises) =>
        new()
        {
            Id = "s1",
            WorkoutPlanId = "plan-1",
            TargetUserId = 10,
            ExecutedByUserId = 10,
            StartedAtUtc = EndedAt.AddHours(-1),
            AppliedSetOperationIds = ["op-1"],
            Exercises = [.. exercises]
        };

    private static ExecutedExerciseDocumentValueObject Exercise(int id, params ExecutedSetDocumentValueObject[] sets) =>
        new() { ExerciseId = id, ExerciseName = $"Ex{id}", Sets = [.. sets] };

    private static Task<ShapeUp.Shared.Results.Result> Finish(FinishWorkoutExecutionHandler sut) =>
        sut.HandleAsync(new FinishWorkoutExecutionCommand("s1", EndedAt, 7, null), 10, CancellationToken.None);

    [Fact]
    public async Task HandleAsync_CreatesTomorrowsSessionWithLoadRegisteredToday()
    {
        // Plan: 2 sets at 50 kg x 10. The user corrected the first to 60 kg and the second to 65 kg.
        var session = Session(Exercise(1, Planned(50m, 10), Planned(50m, 10), Done(60m, 8), Done(65m, 6)));

        var result = await Finish(CreateHandler(session));

        Assert.True(result.IsSuccess);
        Assert.NotNull(_created);
        Assert.NotEqual("s1", _created!.Id);
        Assert.Equal("plan-1", _created.WorkoutPlanId);
        Assert.Equal(10, _created.TargetUserId);
        Assert.Equal(new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc), _created.StartedAtUtc);
        Assert.False(_created.IsCompleted);
        var sets = _created.Exercises.Single().Sets;
        Assert.Equal(2, sets.Count);
        Assert.Equal([60m, 65m], sets.Select(s => s.Load!.Value));
        Assert.Equal([10, 10], sets.Select(s => s.Repetitions!.Value));
        Assert.All(sets, s => Assert.Equal(false, s.IsPerformed));
    }

    [Fact]
    public async Task HandleAsync_KeepsExercisesOrderAndVolumeAndSkipsExtraSets()
    {
        var session = Session(
            Exercise(3, Planned(20m, 12), Planned(20m, 12), Done(25m, 12), Done(25m, 12), Done(25m, 12, extra: true)),
            Exercise(1, Planned(80m, 5), Done(85m, 5)));

        await Finish(CreateHandler(session));

        Assert.Equal([3, 1], _created!.Exercises.Select(e => e.ExerciseId));
        Assert.Equal(2, _created.Exercises[0].Sets.Count);
        Assert.Single(_created.Exercises[1].Sets);
    }

    [Fact]
    public async Task HandleAsync_NotReachedSetsRepeatLastLoadAndUntouchedExerciseKeepsPlan()
    {
        var session = Session(
            Exercise(1, Planned(50m, 10), Planned(50m, 10), Planned(50m, 10), Done(60m, 8)),
            Exercise(2, Planned(30m, 10)));

        await Finish(CreateHandler(session));

        Assert.Equal([60m, 60m, 60m], _created!.Exercises[0].Sets.Select(s => s.Load!.Value));
        Assert.Equal(30m, _created.Exercises[1].Sets.Single().Load);
    }

    [Fact]
    public async Task HandleAsync_WhenNextSessionAlreadyWaiting_DoesNotCreateAnother()
    {
        var session = Session(Exercise(1, Planned(50m, 10), Done(55m, 10)));
        var waiting = new WorkoutSessionDocument { Id = "other", TargetUserId = 10, StartedAtUtc = EndedAt.AddDays(1) };

        var result = await Finish(CreateHandler(session, waiting));

        Assert.True(result.IsSuccess);
        Assert.Null(_created);
    }

    [Fact]
    public async Task HandleAsync_WhenNoSetWasPerformed_CreatesNothing()
    {
        var session = Session(Exercise(1, Planned(50m, 10)));
        session.AppliedSetOperationIds = [];

        var result = await Finish(CreateHandler(session));

        Assert.True(result.IsFailure);
        Assert.Null(_created);
    }
}
