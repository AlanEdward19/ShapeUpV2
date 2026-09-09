namespace IntegrationTests.Domains.Messaging;

using MongoDB.Bson;
using MongoDB.Driver;

public static class OutboxRelayAssertions
{
    public const string OutboxMessagesCollectionName = "outbox.messages";
    public const string OutboxStateCollectionName = "outbox.state";

    public static async Task AssertWorkoutFinishedOutboxRelayedAsync(
        IMongoDatabase database,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        var deadline = DateTime.UtcNow.Add(timeout);

        while (DateTime.UtcNow < deadline)
        {
            if (await IsWorkoutFinishedOutboxRelayedAsync(database, cancellationToken))
                return;

            await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
        }

        var pendingCount = await CountWorkoutFinishedOutboxMessagesAsync(database, cancellationToken);
        Assert.True(
            pendingCount == 0,
            $"Expected WorkoutFinished outbox messages to be removed after relay, but {pendingCount} remain in '{OutboxMessagesCollectionName}'.");
    }

    public static async Task AssertOutboxMessageRelayedAsync(
        IMongoDatabase database,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        var deadline = DateTime.UtcNow.Add(timeout);

        while (DateTime.UtcNow < deadline)
        {
            var pendingCount = await database
                .GetCollection<BsonDocument>(OutboxMessagesCollectionName)
                .CountDocumentsAsync(Builders<BsonDocument>.Filter.Empty, cancellationToken: cancellationToken);

            if (pendingCount == 0)
                return;

            await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
        }

        var remaining = await database
            .GetCollection<BsonDocument>(OutboxMessagesCollectionName)
            .CountDocumentsAsync(Builders<BsonDocument>.Filter.Empty, cancellationToken: cancellationToken);

        Assert.Equal(0, remaining);
    }

    private static async Task<bool> IsWorkoutFinishedOutboxRelayedAsync(
        IMongoDatabase database,
        CancellationToken cancellationToken)
    {
        var pendingCount = await CountWorkoutFinishedOutboxMessagesAsync(database, cancellationToken);
        return pendingCount == 0;
    }

    private static Task<long> CountWorkoutFinishedOutboxMessagesAsync(
        IMongoDatabase database,
        CancellationToken cancellationToken) =>
        database
            .GetCollection<BsonDocument>(OutboxMessagesCollectionName)
            .CountDocumentsAsync(
                Builders<BsonDocument>.Filter.Regex("messageType", new BsonRegularExpression("WorkoutFinished")),
                cancellationToken: cancellationToken);
}
