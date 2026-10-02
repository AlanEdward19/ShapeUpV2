namespace UnitTests.Domains.Training.Workouts;

using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Documents;
using ShapeUp.Features.Training.Shared.Documents.ValueObjects;
using ShapeUp.Features.Training.Workouts.GetTodayWorkoutSession;

public class GetTodayWorkoutSessionHandlerTests
{
    private readonly Mock<IWorkoutSessionRepository> _repository = new();

    private GetTodayWorkoutSessionHandler CreateHandler(WorkoutSessionDocument? active, params WorkoutSessionDocument[] history)
    {
        _repository.Setup(x => x.GetActiveByTargetUserIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(active);
        _repository
            .Setup(x => x.GetCompletedByUserInRangeAsync(10, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);
        return new GetTodayWorkoutSessionHandler(_repository.Object);
    }

    private static WorkoutSessionDocument Today(params ExecutedExerciseDocumentValueObject[] exercises) =>
        new() { TargetUserId = 10, StartedAtUtc = DateTime.UtcNow.AddMinutes(-5), Exercises = [.. exercises] };

    private static ExecutedExerciseDocumentValueObject Exercise(int id, params ExecutedSetDocumentValueObject[] sets) =>
        new() { ExerciseId = id, ExerciseName = $"Ex{id}", Sets = [.. sets] };

    private static ExecutedSetDocumentValueObject PlannedSet(decimal? load = null, int? reps = 10) =>
        new() { Load = load, Repetitions = reps, IsPerformed = false };

    private static ExecutedSetDocumentValueObject DoneSet(decimal load, int reps, bool? performed = true) =>
        new() { Load = load, Repetitions = reps, IsPerformed = performed };

    private static WorkoutSessionDocument Done(int daysAgo, params ExecutedExerciseDocumentValueObject[] exercises) =>
        new() { TargetUserId = 10, IsCompleted = true, StartedAtUtc = DateTime.UtcNow.AddDays(-daysAgo), Exercises = [.. exercises] };

    [Fact]
    public async Task HandleAsync_WithoutOpenSession_ReturnsNoSession()
    {
        var result = await CreateHandler(null).HandleAsync(10, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.HasSession);
        Assert.Null(result.Value.Session);
    }

    [Fact]
    public async Task HandleAsync_WithSessionPreparedForFutureDay_ReturnsNoSession()
    {
        var future = Today(Exercise(1, PlannedSet()));
        future.StartedAtUtc = DateTime.UtcNow.Date.AddDays(1).AddHours(6);

        var result = await CreateHandler(future).HandleAsync(10, CancellationToken.None);

        Assert.False(result.Value!.HasSession);
    }

    [Fact]
    public async Task HandleAsync_AttachesLastLoadFromMostRecentPerformedSession()
    {
        var handler = CreateHandler(
            Today(Exercise(1, PlannedSet(), PlannedSet())),
            Done(2, Exercise(1, DoneSet(60m, 8), DoneSet(65m, 6))),
            Done(9, Exercise(1, DoneSet(40m, 10))));

        var result = await handler.HandleAsync(10, CancellationToken.None);

        var sets = result.Value!.Session!.Exercises.Single().Sets;
        Assert.Equal(60m, sets[0].LastLoad);
        Assert.Equal(8, sets[0].LastRepetitions);
        Assert.Equal(65m, sets[1].LastLoad);
        Assert.Equal(6, sets[1].LastRepetitions);
    }

    [Fact]
    public async Task HandleAsync_ExtraSetsRepeatLastRecordedSet()
    {
        var handler = CreateHandler(
            Today(Exercise(1, PlannedSet(), PlannedSet(), PlannedSet())),
            Done(2, Exercise(1, DoneSet(60m, 8), DoneSet(70m, 5))));

        var sets = (await handler.HandleAsync(10, CancellationToken.None)).Value!.Session!.Exercises.Single().Sets;

        Assert.Equal(70m, sets[2].LastLoad);
    }

    [Fact]
    public async Task HandleAsync_NeverPerformedExercise_HasNullLastValues()
    {
        var handler = CreateHandler(
            Today(Exercise(1, PlannedSet(), PlannedSet()), Exercise(2, PlannedSet())),
            Done(2, Exercise(1, DoneSet(60m, 8))));

        var exercises = (await handler.HandleAsync(10, CancellationToken.None)).Value!.Session!.Exercises;

        Assert.NotNull(exercises[0].Sets[0].LastLoad);
        Assert.Null(exercises[1].Sets[0].LastLoad);
        Assert.Null(exercises[1].Sets[0].LastRepetitions);
    }

    [Fact]
    public async Task HandleAsync_IgnoresPlanPrefilledSetsAndKeepsLegacySets()
    {
        var handler = CreateHandler(
            Today(Exercise(1, PlannedSet(), PlannedSet())),
            Done(1, Exercise(1, DoneSet(100m, 10, performed: false))),
            Done(5, Exercise(1, DoneSet(50m, 12, performed: null))));

        var sets = (await handler.HandleAsync(10, CancellationToken.None)).Value!.Session!.Exercises.Single().Sets;

        Assert.Equal(50m, sets[0].LastLoad);
        Assert.Equal(12, sets[0].LastRepetitions);
    }

    [Fact]
    public async Task HandleAsync_KeepsPlannedExercisesAndOrder()
    {
        var handler = CreateHandler(Today(Exercise(3, PlannedSet()), Exercise(1, PlannedSet())));

        var exercises = (await handler.HandleAsync(10, CancellationToken.None)).Value!.Session!.Exercises;

        Assert.Equal([3, 1], exercises.Select(e => e.ExerciseId));
    }
}
