namespace IntegrationTests.Domains.Messaging;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using IntegrationTests.Infrastructure;

[Collection("SQL Server Write Operations")]
public sealed class WorkoutFinishedInMemoryTransportTests(SqlServerFixture fixture) : IAsyncLifetime
{
    private IntegrationWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        WorkoutFinishedConsumerLogCapture.Reset();
        _factory = new IntegrationWebApplicationFactory(fixture);
        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task FinishWorkout_WithInMemoryTransport_PublishesAndConsumesWorkoutFinished()
    {
        WorkoutFinishedConsumerLogCapture.Reset();

        var owner = await SeedUserAsync("training:equipments:create", "training:exercises:create", "training:workout-plans:create");
        Authorize(owner.Token);
        var exerciseId = await CreateExerciseAsync();
        var planId = await CreatePlanAsync(owner.UserId, exerciseId);
        var sessionId = await StartAndReadSessionIdAsync(planId, owner.UserId);
        var endedAtUtc = DateTime.UtcNow;

        var response = await _client.PostAsJsonAsync($"/api/training/workouts/{sessionId}/finish", new
        {
            endedAtUtc,
            perceivedExertion = 7
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await WaitForConsumerLogAsync(sessionId, TimeSpan.FromSeconds(30));

        Assert.True(WorkoutFinishedConsumerLogCapture.ContainsExpectedPayload(
            sessionId,
            owner.UserId,
            owner.UserId,
            endedAtUtc));
    }

    private static async Task WaitForConsumerLogAsync(string sessionId, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow.Add(timeout);

        while (DateTime.UtcNow < deadline)
        {
            if (WorkoutFinishedConsumerLogCapture.TryGetCaptured(sessionId, out CapturedWorkoutFinishedLog? _))
                return;

            await Task.Delay(TimeSpan.FromMilliseconds(250));
        }

        throw new TimeoutException($"WorkoutFinishedConsumer did not log session {sessionId} within {timeout.TotalSeconds}s.");
    }

    private async Task<string> StartAndReadSessionIdAsync(string planId, int executedByUserId)
    {
        var response = await _client.PostAsJsonAsync("/api/training/workouts/start", new
        {
            planId,
            startedAtUtc = DateTime.UtcNow,
            executedByUserId
        });
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<WorkoutPayload>();
        return payload!.SessionId;
    }

    private async Task<string> CreatePlanAsync(int targetUserId, int exerciseId)
    {
        var response = await _client.PostAsJsonAsync("/api/training/workout-plans", new
        {
            targetUserId,
            name = $"Plan-{Guid.NewGuid():N}",
            notes = "notes",
            durationInWeeks = 4,
            phase = "Hypertrophy",
            difficulty = (int)ShapeUp.Features.Training.Shared.Enums.Difficulty.Intermediate,
            blocks = new[]
            {
                new
                {
                    type = (int)ShapeUp.Features.Training.Shared.Enums.BlockType.Straight,
                    exercises = new[]
                    {
                        new
                        {
                            exerciseId,
                            sets = new[]
                            {
                                new
                                {
                                    repetitions = 10,
                                    load = 20m,
                                    loadUnit = (int)ShapeUp.Features.Training.Shared.Enums.LoadUnit.Kg,
                                    setType = (int)ShapeUp.Features.Training.Shared.Enums.SetType.Working,
                                    technique = (int)ShapeUp.Features.Training.Shared.Enums.Technique.Straight,
                                    intensity = new { type = (int)ShapeUp.Features.Training.Shared.Enums.IntensityType.Rpe, value = 8 },
                                    restSeconds = 90
                                }
                            }
                        }
                    }
                }
            }
        });

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<WorkoutPlanPayload>();
        return payload!.PlanId;
    }

    private async Task<int> CreateExerciseAsync()
    {
        var equipment = await _client.PostAsJsonAsync("/api/training/equipments", new
        {
            name = $"Barbell-{Guid.NewGuid():N}",
            namePt = $"Barra-{Guid.NewGuid():N}",
            description = "Olympic barbell"
        });
        equipment.EnsureSuccessStatusCode();
        var equipmentId = (await equipment.Content.ReadFromJsonAsync<EquipmentPayload>())!.Id;

        var exercise = await _client.PostAsJsonAsync("/api/training/exercises", new
        {
            name = $"Bench Press-{Guid.NewGuid():N}",
            namePt = $"Supino-{Guid.NewGuid():N}",
            description = "Compound press",
            videoUrl = (string?)null,
            muscles = new[] { new { muscleGroup = (int)ShapeUp.Features.Training.Shared.Enums.MuscleGroup.Chest, activationPercent = 70m } },
            equipmentIds = new[] { equipmentId },
            steps = new[] { new { description = "Brace and press" } }
        });
        exercise.EnsureSuccessStatusCode();
        return (await exercise.Content.ReadFromJsonAsync<ExercisePayload>())!.Id;
    }

    private void Authorize(string token)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private async Task<(int UserId, string Token)> SeedUserAsync(params string[] scopes)
    {
        await using var context = fixture.CreateAuthorizationDbContext();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var user = await TestDataSeeder.SeedUserAsync(context, suffix, CancellationToken.None);

        if (scopes.Contains("training:exercises:create") || scopes.Contains("training:equipments:create"))
        {
            await using var gymContext = fixture.CreateGymManagementDbContext();
            await TestDataSeeder.GrantPlatformAdminAsync(gymContext, user.Id, CancellationToken.None);
        }

        return (user.Id, TestFirebaseService.CreateToken(user.FirebaseUid, user.Email));
    }

    private sealed record EquipmentPayload(int Id, string Name, string NamePt, string? Description);
    private sealed record ExercisePayload(int Id, string Name, string NamePt);
    private sealed record WorkoutPlanPayload(string PlanId, int TargetUserId, string Name);
    private sealed record WorkoutPayload(string SessionId, int TargetUserId, bool IsCompleted);
}
