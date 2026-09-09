namespace IntegrationTests.Domains.Gamification;

using System.Net.Http.Json;
using IntegrationTests.Domains.Messaging;
using IntegrationTests.Infrastructure;

[Collection("Messaging")]
public sealed class GamificationEndToEndTests(SqlServerFixture sqlFixture, MessagingInfraFixture _) : IAsyncLifetime
{
    private MessagingIntegrationWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        WorkoutFinishedConsumerLogCapture.Reset();
        _factory = new MessagingIntegrationWebApplicationFactory(sqlFixture);
        _factory.FaultInjector.ThrowAfterPublish = false;
        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task FinishWorkout_EndToEnd_CreditsProfileAndReturnsViaMeEndpoint()
    {
        WorkoutFinishedConsumerLogCapture.Reset();

        var owner = await GamificationIntegrationTestHelper.SeedTrainingUserAsync(sqlFixture);
        GamificationIntegrationTestHelper.Authorize(_client, owner.Token);

        var exerciseId = await GamificationIntegrationTestHelper.CreateExerciseAsync(_client);
        var (sessionId, endedAtUtc) = await GamificationIntegrationTestHelper.FinishPlausibleWorkoutAsync(
            _client,
            owner.UserId,
            exerciseId);

        await GamificationIntegrationTestHelper.WaitForConsumerLogAsync(sessionId, TimeSpan.FromSeconds(45));
        var evaluation = await GamificationIntegrationTestHelper.WaitForEvaluationAsync(
            sqlFixture,
            sessionId,
            TimeSpan.FromSeconds(15));

        await OutboxRelayAssertions.AssertWorkoutFinishedOutboxRelayedAsync(
            new MongoDB.Driver.MongoClient(MessagingInfraFixture.MongoConnectionString)
                .GetDatabase(_factory.MongoDatabaseName),
            TimeSpan.FromSeconds(15));

        var profile = await GamificationIntegrationTestHelper.GetProfileAsync(_client);

        Assert.Equal(50, profile.TotalXp);
        Assert.Equal(10, profile.ShapeCoins);
        Assert.Equal(1, profile.CurrentStreak);
        Assert.Equal(1, profile.Level);
        Assert.True(profile.ShapeScore > 0);

        Assert.True(evaluation.CreditGranted);
        Assert.True(
            evaluation.Classification is ShapeUp.Features.Gamification.Shared.Enums.ActivityClassification.Verified
                or ShapeUp.Features.Gamification.Shared.Enums.ActivityClassification.LikelyValid);

        Assert.True(WorkoutFinishedConsumerLogCapture.ContainsExpectedPayload(
            sessionId,
            owner.UserId,
            owner.UserId,
            endedAtUtc));
    }
}
