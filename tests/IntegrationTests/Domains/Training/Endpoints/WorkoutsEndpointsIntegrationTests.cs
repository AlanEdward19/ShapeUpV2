namespace IntegrationTests.Domains.Training.Endpoints;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Infrastructure;

[Collection("SQL Server Write Operations")]
public sealed class WorkoutsEndpointsIntegrationTests(SqlServerFixture fixture) : IAsyncLifetime
{
    private IntegrationWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        _factory = new IntegrationWebApplicationFactory(fixture);
        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Start_ForSelf_Succeeds()
    {
        var owner = await SeedUserAsync("training:equipments:create", "training:exercises:create", "training:workout-plans:create");
        Authorize(owner.Token);
        var exerciseId = await CreateExerciseAsync();
        var planId = await CreatePlanAsync(owner.UserId, exerciseId);

        var response = await StartWorkoutAsync(planId, owner.UserId);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Start_ForUnrelatedUser_ReturnsForbidden()
    {
        var owner = await SeedUserAsync("training:equipments:create", "training:exercises:create", "training:workout-plans:create");
        var stranger = await SeedUserAsync("training:workout-plans:create");
        Authorize(owner.Token);
        var exerciseId = await CreateExerciseAsync();

        // The plan must target the stranger, and only the stranger (self-access) or someone with an
        // active relationship to them can create it — so it is seeded under the stranger's own auth.
        Authorize(stranger.Token);
        var planId = await CreatePlanAsync(stranger.UserId, exerciseId);

        Authorize(owner.Token);
        var response = await StartWorkoutAsync(planId, stranger.UserId);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetById_OwnerAccessing_Succeeds()
    {
        var owner = await SeedUserAsync("training:equipments:create", "training:exercises:create", "training:workout-plans:create");
        Authorize(owner.Token);
        var exerciseId = await CreateExerciseAsync();
        var planId = await CreatePlanAsync(owner.UserId, exerciseId);
        var sessionId = await StartAndReadSessionIdAsync(planId, owner.UserId);

        var response = await _client.GetAsync($"/api/training/workouts/{sessionId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetById_NonOwnerAccessing_ReturnsForbidden()
    {
        var owner = await SeedUserAsync("training:equipments:create", "training:exercises:create", "training:workout-plans:create");
        var stranger = await SeedUserAsync();
        Authorize(owner.Token);
        var exerciseId = await CreateExerciseAsync();
        var planId = await CreatePlanAsync(owner.UserId, exerciseId);
        var sessionId = await StartAndReadSessionIdAsync(planId, owner.UserId);

        Authorize(stranger.Token);
        var response = await _client.GetAsync($"/api/training/workouts/{sessionId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Finish_OwnerAccessing_Succeeds()
    {
        var owner = await SeedUserAsync("training:equipments:create", "training:exercises:create", "training:workout-plans:create");
        Authorize(owner.Token);
        var exerciseId = await CreateExerciseAsync();
        var planId = await CreatePlanAsync(owner.UserId, exerciseId);
        var sessionId = await StartAndReadSessionIdAsync(planId, owner.UserId);

        var response = await _client.PostAsJsonAsync($"/api/training/workouts/{sessionId}/finish", new
        {
            endedAtUtc = DateTime.UtcNow,
            perceivedExertion = 7
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Finish_NonOwnerAccessing_ReturnsForbidden()
    {
        var owner = await SeedUserAsync("training:equipments:create", "training:exercises:create", "training:workout-plans:create");
        var stranger = await SeedUserAsync();
        Authorize(owner.Token);
        var exerciseId = await CreateExerciseAsync();
        var planId = await CreatePlanAsync(owner.UserId, exerciseId);
        var sessionId = await StartAndReadSessionIdAsync(planId, owner.UserId);

        Authorize(stranger.Token);
        var response = await _client.PostAsJsonAsync($"/api/training/workouts/{sessionId}/finish", new
        {
            endedAtUtc = DateTime.UtcNow,
            perceivedExertion = 7
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Cancel_OwnerAccessing_Succeeds()
    {
        var owner = await SeedUserAsync("training:equipments:create", "training:exercises:create", "training:workout-plans:create");
        Authorize(owner.Token);
        var exerciseId = await CreateExerciseAsync();
        var planId = await CreatePlanAsync(owner.UserId, exerciseId);
        var sessionId = await StartAndReadSessionIdAsync(planId, owner.UserId);

        var response = await _client.PostAsync($"/api/training/workouts/{sessionId}/cancel", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Cancel_NonOwnerAccessing_ReturnsForbidden()
    {
        var owner = await SeedUserAsync("training:equipments:create", "training:exercises:create", "training:workout-plans:create");
        var stranger = await SeedUserAsync();
        Authorize(owner.Token);
        var exerciseId = await CreateExerciseAsync();
        var planId = await CreatePlanAsync(owner.UserId, exerciseId);
        var sessionId = await StartAndReadSessionIdAsync(planId, owner.UserId);

        Authorize(stranger.Token);
        var response = await _client.PostAsync($"/api/training/workouts/{sessionId}/cancel", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SaveState_OwnerAccessing_Succeeds()
    {
        var owner = await SeedUserAsync("training:equipments:create", "training:exercises:create", "training:workout-plans:create");
        Authorize(owner.Token);
        var exerciseId = await CreateExerciseAsync();
        var planId = await CreatePlanAsync(owner.UserId, exerciseId);
        var sessionId = await StartAndReadSessionIdAsync(planId, owner.UserId);

        var response = await _client.PutAsJsonAsync($"/api/training/workouts/{sessionId}/state", new
        {
            savedAtUtc = DateTime.UtcNow,
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
                            restSeconds = 90,
                            isExtra = false
                        }
                    }
                }
            }
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SaveState_NonOwnerAccessing_ReturnsForbidden()
    {
        var owner = await SeedUserAsync("training:equipments:create", "training:exercises:create", "training:workout-plans:create");
        var stranger = await SeedUserAsync();
        Authorize(owner.Token);
        var exerciseId = await CreateExerciseAsync();
        var planId = await CreatePlanAsync(owner.UserId, exerciseId);
        var sessionId = await StartAndReadSessionIdAsync(planId, owner.UserId);

        Authorize(stranger.Token);
        var response = await _client.PutAsJsonAsync($"/api/training/workouts/{sessionId}/state", new
        {
            savedAtUtc = DateTime.UtcNow,
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
                            restSeconds = 90,
                            isExtra = false
                        }
                    }
                }
            }
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetByUser_ForSelf_Succeeds()
    {
        var owner = await SeedUserAsync("training:equipments:create", "training:exercises:create", "training:workout-plans:create");
        Authorize(owner.Token);
        var exerciseId = await CreateExerciseAsync();
        var planId = await CreatePlanAsync(owner.UserId, exerciseId);
        await StartAndReadSessionIdAsync(planId, owner.UserId);

        var response = await _client.GetAsync($"/api/training/workouts/user/{owner.UserId}?pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetByUser_ForUnrelatedUser_ReturnsForbidden()
    {
        var owner = await SeedUserAsync("training:equipments:create", "training:exercises:create", "training:workout-plans:create");
        var stranger = await SeedUserAsync();
        Authorize(owner.Token);

        var response = await _client.GetAsync($"/api/training/workouts/user/{stranger.UserId}?pageSize=10");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetMyActive_ForSelf_Succeeds()
    {
        var owner = await SeedUserAsync();
        Authorize(owner.Token);

        var response = await _client.GetAsync("/api/training/workouts/me/active");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task<HttpResponseMessage> StartWorkoutAsync(string planId, int executedByUserId)
    {
        return await _client.PostAsJsonAsync("/api/training/workouts/start", new
        {
            planId,
            startedAtUtc = DateTime.UtcNow,
            executedByUserId
        });
    }

    private async Task<string> StartAndReadSessionIdAsync(string planId, int executedByUserId)
    {
        var response = await StartWorkoutAsync(planId, executedByUserId);
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

        // native-authorization-model: Exercises/Equipments Create now require PlatformRoleType.Admin
        // instead of a Scope. This helper only ever seeds fixture data (an exercise to build a
        // workout plan around), never tests the catalog endpoints' own authorization boundary.
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
