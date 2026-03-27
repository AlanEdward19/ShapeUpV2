namespace IntegrationTests.Domains.Training.Endpoints;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using ShapeUp.Features.Authorization.Shared.Entities;

[Collection("SQL Server Write Operations")]
public sealed class TrainingEndpointsIntegrationTests(SqlServerFixture fixture) : IAsyncLifetime
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

    [Theory]
    [InlineData("Barbell", "Barra", "Olympic barbell")]
    [InlineData("Dumbbell", "Haltere", "Fixed weight dumbbell")]
    [InlineData("Cable Machine", "Máquina de Cabo", "Adjustable cable machine")]
    [InlineData("Smith Machine", "Máquina Smith", "Guided barbell machine")]
    [InlineData("Leg Press", "Prensa de Perna", "Plate loaded leg press")]
    public async Task EquipmentEndpoints_ShouldCreateGetListUpdateAndDelete(string equipmentName, string equipmentPt, string description)
    {
        var auth = await SeedAuthorizedUserAsync(
            "training:equipments:create",
            "training:equipments:read",
            "training:equipments:update",
            "training:equipments:delete");
        Authorize(auth.Token);

        var create = await _client.PostAsJsonAsync("/api/training/equipments", new
        {
            name = $"{equipmentName}-{Guid.NewGuid():N}",
            namePt = $"{equipmentPt}-{Guid.NewGuid():N}",
            description = description
        });

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = await create.Content.ReadFromJsonAsync<EquipmentPayload>();
        Assert.NotNull(created);

        var get = await _client.GetAsync($"/api/training/equipments/{created!.Id}");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);

        var list = await _client.GetAsync("/api/training/equipments?pageSize=10");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);

        var update = await _client.PutAsJsonAsync($"/api/training/equipments/{created.Id}", new
        {
            equipmentId = created.Id,
            name = $"{equipmentName}Updated-{Guid.NewGuid():N}",
            namePt = $"{equipmentPt}Atualizado-{Guid.NewGuid():N}",
            description = "Updated"
        });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var delete = await _client.DeleteAsync($"/api/training/equipments/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, delete.StatusCode);
    }

    [Theory]
    [InlineData("Bench Press", "Supino", "https://example.com/bench")]
    [InlineData("Squat", "Agachamento", "https://example.com/squat")]
    [InlineData("Deadlift", "Rosca Direta", "https://example.com/deadlift")]
    [InlineData("Pull-ups", "Puxada", "https://example.com/pullups")]
    public async Task ExerciseEndpoints_ShouldCreateSuggestUpdateAndGet(string exerciseName, string exerciseNamePt, string videoUrl)
    {
        var auth = await SeedAuthorizedUserAsync(
            "training:equipments:create",
            "training:exercises:create",
            "training:exercises:read",
            "training:exercises:update",
            "training:exercises:suggest");
        Authorize(auth.Token);

        var equipment = await CreateEquipmentAsync();

        var create = await _client.PostAsJsonAsync("/api/training/exercises", new
        {
            name = $"{exerciseName}-{Guid.NewGuid():N}",
            namePt = $"{exerciseNamePt}-{Guid.NewGuid():N}",
            description = "Compound press",
            videoUrl = videoUrl,
            muscles = new[]
            {
                new { muscleGroup = (int)ShapeUp.Features.Training.Shared.Enums.EMuscleGroup.Chest, activationPercent = 70m }
            },
            equipmentIds = new[] { equipment.Id },
            steps = new[]
            {
                new { description = "Drive feet into the floor" }
            }
        });

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = await create.Content.ReadFromJsonAsync<ExercisePayload>();
        Assert.NotNull(created);
        Assert.Single(created!.Muscles);
        Assert.Single(created.Equipments);

        var get = await _client.GetAsync($"/api/training/exercises/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);

        var suggest = await _client.PostAsJsonAsync("/api/training/exercises/suggest", new
        {
            name = exerciseNamePt.ToLower(),
            muscleGroups = new[] { (int)ShapeUp.Features.Training.Shared.Enums.EMuscleGroup.Chest },
            equipmentIds = new[] { equipment.Id },
            limit = 5
        });
        Assert.Equal(HttpStatusCode.OK, suggest.StatusCode);
        var suggestions = await suggest.Content.ReadFromJsonAsync<ExercisePayload[]>();
        Assert.NotNull(suggestions);
        Assert.NotEmpty(suggestions!);

        var update = await _client.PutAsJsonAsync($"/api/training/exercises/{created.Id}", new
        {
            exerciseId = created.Id,
            name = $"Incline {exerciseName}-{Guid.NewGuid():N}",
            namePt = $"{exerciseNamePt} Inclinado-{Guid.NewGuid():N}",
            description = "Updated",
            videoUrl = "https://example.com/video2",
            muscles = new[]
            {
                new { muscleGroup = (int)ShapeUp.Features.Training.Shared.Enums.EMuscleGroup.Chest, activationPercent = 75m }
            },
            equipmentIds = new[] { equipment.Id },
            steps = new[]
            {
                new { description = "Control the eccentric" }
            }
        });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
    }

    [Theory]
    [InlineData("working", 8, 100, 8, 120)]
    [InlineData("warmup", 12, 50, 3, 60)]
    [InlineData("topset", 5, 120, 9, 180)]
    [InlineData("dropset", 8, 80, 7, 90)]
    [InlineData("backoff", 10, 70, 6, 120)]
    public async Task WorkoutEndpoints_AndDashboard_ShouldCreateReadListCompleteAndSummarize(
        string setType, int reps, decimal load, int rpe, int restSeconds)
    {
        var auth = await SeedAuthorizedUserAsync(
            "training:equipments:create",
            "training:exercises:create",
            "training:exercises:read",
            "training:workouts:create",
            "training:workouts:create:self",
            "training:workouts:read",
            "training:workouts:complete",
            "training:dashboard:read");
        Authorize(auth.Token);

        var equipment = await CreateEquipmentAsync();
        var exercise = await CreateExerciseAsync(equipment.Id);

        var startedAtUtc = DateTime.UtcNow.AddMinutes(-30);
        var createWorkout = await _client.PostAsJsonAsync("/api/training/workouts", new
        {
            targetUserId = auth.UserId,
            executedByUserId = auth.UserId,
            startedAtUtc,
            exercises = new[]
            {
                new
                {
                    exerciseId = exercise.Id,
                    sets = new[]
                    {
                        new { repetitions = reps, load = load, loadUnit = "kg", setType = setType, rpe = rpe, restSeconds = restSeconds }
                    }
                }
            }
        });

        Assert.Equal(HttpStatusCode.Created, createWorkout.StatusCode);
        var createdWorkout = await createWorkout.Content.ReadFromJsonAsync<WorkoutPayload>();
        Assert.NotNull(createdWorkout);

        var getById = await _client.GetAsync($"/api/training/workouts/{createdWorkout!.SessionId}");
        Assert.Equal(HttpStatusCode.OK, getById.StatusCode);

        var getByUser = await _client.GetAsync($"/api/training/workouts/user/{auth.UserId}?pageSize=10");
        Assert.Equal(HttpStatusCode.OK, getByUser.StatusCode);

        var complete = await _client.PostAsJsonAsync($"/api/training/workouts/{createdWorkout.SessionId}/complete", new
        {
            sessionId = createdWorkout.SessionId,
            endedAtUtc = DateTime.UtcNow,
            perceivedExertion = rpe
        });
        Assert.Equal(HttpStatusCode.OK, complete.StatusCode);

        var dashboard = await _client.GetAsync("/api/training/dashboard/me?sessionsTargetPerWeek=3");
        Assert.Equal(HttpStatusCode.OK, dashboard.StatusCode);
        var dashboardPayload = await dashboard.Content.ReadFromJsonAsync<DashboardPayload>();
        Assert.NotNull(dashboardPayload);
        Assert.True(dashboardPayload!.WeeklyVolume >= 0);
        Assert.Equal(1, dashboardPayload.SessionsCompletedThisWeek);
    }

    [Theory]
    [InlineData("training:equipments:read")]
    [InlineData("training:exercises:read")]
    [InlineData("training:dashboard:read")]
    public async Task TrainingEndpoints_ShouldRespectScopes(string validScope)
    {
        var auth = await SeedAuthorizedUserAsync(validScope);
        Authorize(auth.Token);

        var response = await _client.PostAsJsonAsync("/api/training/equipments", new
        {
            name = "Barbell",
            namePt = "Barra",
            description = "Olympic barbell"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("  ", "  ")]
    [InlineData("A", "")]
    [InlineData("", "B")]
    public async Task TrainingEndpoints_InvalidPayload_ShouldReturnBadRequest(string name, string namePt)
    {
        var auth = await SeedAuthorizedUserAsync("training:equipments:create");
        Authorize(auth.Token);

        var response = await _client.PostAsJsonAsync("/api/training/equipments", new
        {
            name = name,
            namePt = namePt,
            description = "Test"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private void Authorize(string token)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private async Task<AuthorizedUser> SeedAuthorizedUserAsync(params string[] scopes)
    {
        await using var context = fixture.CreateAuthorizationDbContext();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var user = await TestDataSeeder.SeedUserAsync(context, suffix, CancellationToken.None);
        await TestDataSeeder.AssignScopesToUserAsync(context, user.Id, scopes);

        return new AuthorizedUser(user.Id, TestFirebaseService.CreateToken(user.FirebaseUid, user.Email));
    }

    private async Task<EquipmentPayload> CreateEquipmentAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/training/equipments", new
        {
            name = $"Barbell-{Guid.NewGuid():N}",
            namePt = $"Barra-{Guid.NewGuid():N}",
            description = "Olympic barbell"
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EquipmentPayload>())!;
    }

    private async Task<ExercisePayload> CreateExerciseAsync(int equipmentId)
    {
        var response = await _client.PostAsJsonAsync("/api/training/exercises", new
        {
            name = $"Bench Press-{Guid.NewGuid():N}",
            namePt = $"Supino-{Guid.NewGuid():N}",
            description = "Compound press",
            videoUrl = (string?)null,
            muscles = new[] { new { muscleGroup = (int)ShapeUp.Features.Training.Shared.Enums.EMuscleGroup.Chest, activationPercent = 70m } },
            equipmentIds = new[] { equipmentId },
            steps = new[] { new { description = "Brace and press" } }
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ExercisePayload>())!;
    }

    private sealed record AuthorizedUser(int UserId, string Token);
    private sealed record EquipmentPayload(int Id, string Name, string NamePt, string? Description);
    private sealed record ExercisePayload(int Id, string Name, string NamePt, ExerciseMusclePayload[] Muscles, ExerciseEquipmentPayload[] Equipments, string[] Steps);
    private sealed record ExerciseMusclePayload(long MuscleGroup, string MuscleName, string MuscleNamePt, decimal ActivationPercent);
    private sealed record ExerciseEquipmentPayload(int EquipmentId, string EquipmentName, string EquipmentNamePt);
    private sealed record WorkoutPayload(string SessionId, int TargetUserId, bool IsCompleted);
    private sealed record DashboardPayload(decimal WeeklyVolume, int ConsecutiveTrainingDays, int SessionsCompletedThisWeek, int SessionsTargetPerWeek, decimal SessionsCompletionRate, int PersonalRecordsThisWeek, decimal WeeklyVolumeProgressPercent);
}

