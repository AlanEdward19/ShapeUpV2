using MassTransit;
using Microsoft.Extensions.Logging;
using ShapeUp.Features.Training.Shared.Events;
using ShapeUp.Features.Training.Workouts.FinishWorkoutExecution;

namespace UnitTests.Domains.Training.Workouts;

public class WorkoutFinishedConsumerTests
{
    [Fact]
    public async Task Consume_LogsWorkoutFinishedEventWithExpectedFields()
    {
        var endedAtUtc = new DateTime(2026, 3, 29, 10, 0, 0, DateTimeKind.Utc);
        var message = new WorkoutFinished("session-42", 10, 17, endedAtUtc);

        var context = new Mock<ConsumeContext<WorkoutFinished>>();
        context.Setup(x => x.Message).Returns(message);

        var logger = new Mock<ILogger<WorkoutFinishedConsumer>>();
        var sut = new WorkoutFinishedConsumer(logger.Object);

        await sut.Consume(context.Object);

        logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) =>
                    MatchesExpectedLog(state.ToString()!)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private static bool MatchesExpectedLog(string formatted) =>
        formatted.Contains("session-42", StringComparison.Ordinal)
        && formatted.Contains("target user 10", StringComparison.Ordinal)
        && formatted.Contains("executed by 17", StringComparison.Ordinal)
        && formatted.Contains("03/29/2026 10:00:00", StringComparison.Ordinal);
}
