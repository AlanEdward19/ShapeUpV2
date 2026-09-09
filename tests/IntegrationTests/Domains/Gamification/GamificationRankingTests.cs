namespace IntegrationTests.Domains.Gamification;

using IntegrationTests.Domains.Messaging;
using IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;

[Collection("Messaging")]
public sealed class GamificationRankingTests(SqlServerFixture sqlFixture, MessagingInfraFixture _) : IAsyncLifetime
{
    private MessagingIntegrationWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;
    private IMongoDatabase _mongoDatabase = null!;

    public Task InitializeAsync()
    {
        WorkoutFinishedConsumerLogCapture.Reset();
        _factory = new MessagingIntegrationWebApplicationFactory(sqlFixture);
        _factory.FaultInjector.ThrowAfterPublish = false;
        _client = _factory.CreateClient();
        _mongoDatabase = new MongoClient(MessagingInfraFixture.MongoConnectionString)
            .GetDatabase(_factory.MongoDatabaseName);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task GetRanking_ReturnsUsersOrderedByShapeScoreDesc_WithKeysetPagination()
    {
        var participants = new List<(int UserId, string Token)>();
        foreach (var verifiedWorkoutDays in new[] { 10, 7, 4, 1 })
        {
            var participant = await GamificationIntegrationTestHelper.SeedRankingParticipantAsync(
                sqlFixture,
                _mongoDatabase,
                verifiedWorkoutDays);
            participants.Add(participant);
        }

        GamificationIntegrationTestHelper.Authorize(_client, participants[0].Token);

        var participantIds = participants.Select(p => p.UserId).ToHashSet();
        var firstPage = await GamificationIntegrationTestHelper.GetRankingAsync(_client, pageSize: 2);
        Assert.Equal(2, firstPage.Items.Length);
        Assert.NotNull(firstPage.NextCursor);
        Assert.True(firstPage.Items[0].ShapeScore >= firstPage.Items[1].ShapeScore);

        var collected = new List<GamificationIntegrationTestHelper.RankingEntryPayload>();
        string? cursor = null;
        var pagesVisited = 0;

        while (pagesVisited < 20)
        {
            var page = await GamificationIntegrationTestHelper.GetRankingAsync(_client, pageSize: 2, cursor);
            pagesVisited++;

            if (page.Items.Length == 0)
                break;

            if (page.Items.Length == 2)
                Assert.True(page.Items[0].ShapeScore >= page.Items[1].ShapeScore);

            collected.AddRange(page.Items.Where(item => participantIds.Contains(item.UserId)));

            if (page.NextCursor is null)
                break;

            cursor = page.NextCursor;
        }

        Assert.True(pagesVisited >= 2);
        Assert.Equal(4, collected.Count);

        var expectedParticipantOrder = collected
            .OrderByDescending(item => item.ShapeScore)
            .ThenBy(item => item.UserId)
            .Select(item => item.UserId)
            .ToArray();

        Assert.Equal(expectedParticipantOrder, collected.Select(item => item.UserId).ToArray());
        Assert.True(collected.Select(item => item.ShapeScore).Distinct().Count() >= 3);
    }
}
