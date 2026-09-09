namespace IntegrationTests.Domains.Gamification;

using IntegrationTests.Domains.Messaging;
using IntegrationTests.Infrastructure;
using ShapeUp.Features.Gamification.Shared.Enums;

[Collection("Messaging")]
public sealed class GamificationAntiCheatEndToEndTests(SqlServerFixture sqlFixture, MessagingInfraFixture _) : IAsyncLifetime
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
    public async Task ImpossibleDurationWorkout_DoesNotCreditAndPersistsInvalidClassification()
    {
        WorkoutFinishedConsumerLogCapture.Reset();

        var owner = await GamificationIntegrationTestHelper.SeedTrainingUserAsync(sqlFixture);
        GamificationIntegrationTestHelper.Authorize(_client, owner.Token);

        var exerciseId = await GamificationIntegrationTestHelper.CreateExerciseAsync(_client);
        var planId = await GamificationIntegrationTestHelper.CreatePlanAsync(_client, owner.UserId, exerciseId);
        var endedAtUtc = DateTime.UtcNow;
        var startedAtUtc = endedAtUtc.AddSeconds(-5);
        var sessionId = await GamificationIntegrationTestHelper.StartSessionAsync(
            _client,
            planId,
            owner.UserId,
            startedAtUtc);

        await GamificationIntegrationTestHelper.FinishSessionAsync(_client, sessionId, endedAtUtc);
        await GamificationIntegrationTestHelper.WaitForConsumerLogAsync(sessionId, TimeSpan.FromSeconds(45));

        var evaluation = await GamificationIntegrationTestHelper.WaitForEvaluationAsync(
            sqlFixture,
            sessionId,
            TimeSpan.FromSeconds(15));

        var profile = await GamificationIntegrationTestHelper.GetProfileAsync(_client);

        Assert.Equal(0, profile.TotalXp);
        Assert.Equal(0, profile.ShapeCoins);
        GamificationIntegrationTestHelper.AssertClassification(evaluation, ActivityClassification.Invalid, false);
    }

    [Fact]
    public async Task ExactDuplicateWorkout_SecondFinishDoesNotCreditAndPersistsInvalidClassification()
    {
        WorkoutFinishedConsumerLogCapture.Reset();

        var owner = await GamificationIntegrationTestHelper.SeedTrainingUserAsync(sqlFixture);
        GamificationIntegrationTestHelper.Authorize(_client, owner.Token);

        var exerciseId = await GamificationIntegrationTestHelper.CreateExerciseAsync(_client);
        var (firstSessionId, firstEndedAtUtc) = await GamificationIntegrationTestHelper.FinishPlausibleWorkoutAsync(
            _client,
            owner.UserId,
            exerciseId);

        await GamificationIntegrationTestHelper.WaitForConsumerLogAsync(firstSessionId, TimeSpan.FromSeconds(45));
        await GamificationIntegrationTestHelper.WaitForEvaluationAsync(sqlFixture, firstSessionId, TimeSpan.FromSeconds(15));

        var profileAfterFirst = await GamificationIntegrationTestHelper.GetProfileAsync(_client);
        Assert.Equal(50, profileAfterFirst.TotalXp);
        Assert.Equal(10, profileAfterFirst.ShapeCoins);

        var secondEndedAtUtc = firstEndedAtUtc.AddMinutes(2);
        var (secondSessionId, _) = await GamificationIntegrationTestHelper.FinishPlausibleWorkoutAsync(
            _client,
            owner.UserId,
            exerciseId,
            endedAtUtc: secondEndedAtUtc);

        await GamificationIntegrationTestHelper.WaitForConsumerLogAsync(secondSessionId, TimeSpan.FromSeconds(45));

        var secondEvaluation = await GamificationIntegrationTestHelper.WaitForEvaluationAsync(
            sqlFixture,
            secondSessionId,
            TimeSpan.FromSeconds(15));

        var profileAfterSecond = await GamificationIntegrationTestHelper.GetProfileAsync(_client);

        Assert.Equal(profileAfterFirst.TotalXp, profileAfterSecond.TotalXp);
        Assert.Equal(profileAfterFirst.ShapeCoins, profileAfterSecond.ShapeCoins);
        GamificationIntegrationTestHelper.AssertClassification(secondEvaluation, ActivityClassification.Invalid, false);

        var firstEvaluation = await GamificationIntegrationTestHelper.GetEvaluationAsync(sqlFixture, firstSessionId);
        Assert.NotNull(firstEvaluation);
        Assert.True(firstEvaluation!.CreditGranted);
    }

    [Fact]
    public async Task VolumeSpikeWorkout_DoesNotCreditAndPersistsSuspiciousClassification()
    {
        WorkoutFinishedConsumerLogCapture.Reset();

        var owner = await GamificationIntegrationTestHelper.SeedTrainingUserAsync(sqlFixture);
        GamificationIntegrationTestHelper.Authorize(_client, owner.Token);

        var exerciseId = await GamificationIntegrationTestHelper.CreateExerciseAsync(_client);
        const decimal baselineLoad = 10m;
        const decimal spikeLoad = 40m;

        for (var index = 0; index < 3; index++)
        {
            var endedAtUtc = DateTime.UtcNow.AddDays(-(3 - index));
            var (sessionId, _) = await GamificationIntegrationTestHelper.FinishPlausibleWorkoutAsync(
                _client,
                owner.UserId,
                exerciseId,
                load: baselineLoad,
                endedAtUtc: endedAtUtc);

            await GamificationIntegrationTestHelper.WaitForConsumerLogAsync(sessionId, TimeSpan.FromSeconds(45));
            await GamificationIntegrationTestHelper.WaitForEvaluationAsync(sqlFixture, sessionId, TimeSpan.FromSeconds(15));
            WorkoutFinishedConsumerLogCapture.Reset();
        }

        var profileBeforeSpike = await GamificationIntegrationTestHelper.GetProfileAsync(_client);

        var spikeExercises = GamificationIntegrationTestHelper.BuildFinishExercises(
            exerciseId,
            spikeLoad,
            GamificationIntegrationTestHelper.DefaultRepetitions);

        var (spikeSessionId, _) = await GamificationIntegrationTestHelper.FinishPlausibleWorkoutAsync(
            _client,
            owner.UserId,
            exerciseId,
            load: baselineLoad,
            finishExercises: spikeExercises);

        await GamificationIntegrationTestHelper.WaitForConsumerLogAsync(spikeSessionId, TimeSpan.FromSeconds(45));

        var spikeEvaluation = await GamificationIntegrationTestHelper.WaitForEvaluationAsync(
            sqlFixture,
            spikeSessionId,
            TimeSpan.FromSeconds(15));

        var profileAfterSpike = await GamificationIntegrationTestHelper.GetProfileAsync(_client);

        Assert.Equal(profileBeforeSpike.TotalXp, profileAfterSpike.TotalXp);
        Assert.Equal(profileBeforeSpike.ShapeCoins, profileAfterSpike.ShapeCoins);
        GamificationIntegrationTestHelper.AssertClassification(spikeEvaluation, ActivityClassification.Suspicious, false);
    }
}
