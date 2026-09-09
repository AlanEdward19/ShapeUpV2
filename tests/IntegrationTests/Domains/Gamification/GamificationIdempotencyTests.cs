namespace IntegrationTests.Domains.Gamification;

using IntegrationTests.Domains.Messaging;
using IntegrationTests.Infrastructure;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ShapeUp.Features.Training.Shared.Events;

[Collection("Messaging")]
public sealed class GamificationIdempotencyTests(SqlServerFixture sqlFixture, MessagingInfraFixture _) : IAsyncLifetime
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
    public async Task RedeliveredWorkoutFinished_CreditsProfileExactlyOnce()
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
        await GamificationIntegrationTestHelper.WaitForEvaluationAsync(sqlFixture, sessionId, TimeSpan.FromSeconds(15));

        var profileAfterFirstDelivery = await GamificationIntegrationTestHelper.GetProfileAsync(_client);
        Assert.Equal(50, profileAfterFirstDelivery.TotalXp);
        Assert.Equal(10, profileAfterFirstDelivery.ShapeCoins);

        var message = new WorkoutFinished(sessionId, owner.UserId, owner.UserId, endedAtUtc);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

            for (var attempt = 0; attempt < 2; attempt++)
                await publishEndpoint.Publish(message, CancellationToken.None);
        }

        await Task.Delay(TimeSpan.FromSeconds(2));

        var profileAfterRedelivery = await GamificationIntegrationTestHelper.GetProfileAsync(_client);

        Assert.Equal(profileAfterFirstDelivery.TotalXp, profileAfterRedelivery.TotalXp);
        Assert.Equal(profileAfterFirstDelivery.ShapeCoins, profileAfterRedelivery.ShapeCoins);
        Assert.Equal(profileAfterFirstDelivery.CurrentStreak, profileAfterRedelivery.CurrentStreak);
        Assert.Equal(profileAfterFirstDelivery.Level, profileAfterRedelivery.Level);

        await using var context = sqlFixture.CreateGamificationDbContext();
        Assert.Equal(1, await context.Evaluations.CountAsync(e => e.SessionId == sessionId));
    }
}
