namespace IntegrationTests.Domains.Training.Endpoints;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Infrastructure;

[Collection("SQL Server Write Operations")]
public sealed class WorkoutTemplatesEndpointsIntegrationTests(SqlServerFixture fixture) : IAsyncLifetime
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
    public async Task Assign_ToSelf_Succeeds()
    {
        var owner = await SeedUserAsync("training:equipments:create", "training:exercises:create");
        Authorize(owner.Token);
        var exerciseId = await CreateExerciseAsync();
        var templateId = await CreateTemplateAsync(exerciseId);

        var response = await _client.PostAsJsonAsync(
            $"/api/training/workout-templates/{templateId}/assign/{owner.UserId}",
            new { planName = (string?)null });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Assign_ToUnrelatedUser_ReturnsForbidden()
    {
        var owner = await SeedUserAsync("training:equipments:create", "training:exercises:create");
        var stranger = await SeedUserAsync();
        Authorize(owner.Token);
        var exerciseId = await CreateExerciseAsync();
        var templateId = await CreateTemplateAsync(exerciseId);

        var response = await _client.PostAsJsonAsync(
            $"/api/training/workout-templates/{templateId}/assign/{stranger.UserId}",
            new { planName = (string?)null });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetById_OwnerAccessing_Succeeds()
    {
        var owner = await SeedUserAsync("training:equipments:create", "training:exercises:create");
        Authorize(owner.Token);
        var exerciseId = await CreateExerciseAsync();
        var templateId = await CreateTemplateAsync(exerciseId);

        var response = await _client.GetAsync($"/api/training/workout-templates/{templateId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetById_NonOwnerAccessing_ReturnsForbidden()
    {
        var owner = await SeedUserAsync("training:equipments:create", "training:exercises:create");
        var stranger = await SeedUserAsync();
        Authorize(owner.Token);
        var exerciseId = await CreateExerciseAsync();
        var templateId = await CreateTemplateAsync(exerciseId);

        Authorize(stranger.Token);
        var response = await _client.GetAsync($"/api/training/workout-templates/{templateId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Update_OwnerAccessing_Succeeds()
    {
        var owner = await SeedUserAsync("training:equipments:create", "training:exercises:create");
        Authorize(owner.Token);
        var exerciseId = await CreateExerciseAsync();
        var templateId = await CreateTemplateAsync(exerciseId);

        var response = await _client.PutAsJsonAsync($"/api/training/workout-templates/{templateId}", UpdateBody(exerciseId));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Update_NonOwnerAccessing_ReturnsForbidden()
    {
        var owner = await SeedUserAsync("training:equipments:create", "training:exercises:create");
        var stranger = await SeedUserAsync();
        Authorize(owner.Token);
        var exerciseId = await CreateExerciseAsync();
        var templateId = await CreateTemplateAsync(exerciseId);

        Authorize(stranger.Token);
        var response = await _client.PutAsJsonAsync($"/api/training/workout-templates/{templateId}", UpdateBody(exerciseId));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Delete_OwnerAccessing_Succeeds()
    {
        var owner = await SeedUserAsync("training:equipments:create", "training:exercises:create");
        Authorize(owner.Token);
        var exerciseId = await CreateExerciseAsync();
        var templateId = await CreateTemplateAsync(exerciseId);

        var response = await _client.DeleteAsync($"/api/training/workout-templates/{templateId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Delete_NonOwnerAccessing_ReturnsForbidden()
    {
        var owner = await SeedUserAsync("training:equipments:create", "training:exercises:create");
        var stranger = await SeedUserAsync();
        Authorize(owner.Token);
        var exerciseId = await CreateExerciseAsync();
        var templateId = await CreateTemplateAsync(exerciseId);

        Authorize(stranger.Token);
        var response = await _client.DeleteAsync($"/api/training/workout-templates/{templateId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static object UpdateBody(int exerciseId) => new
    {
        name = $"Template Updated-{Guid.NewGuid():N}",
        notes = "updated notes",
        durationInWeeks = 6,
        phase = "Strength",
        difficulty = (int)ShapeUp.Features.Training.Shared.Enums.Difficulty.Advanced,
        exercises = new[]
        {
            new
            {
                exerciseId,
                sets = new[]
                {
                    new
                    {
                        repetitions = 8,
                        load = 40m,
                        loadUnit = (int)ShapeUp.Features.Training.Shared.Enums.LoadUnit.Kg,
                        setType = (int)ShapeUp.Features.Training.Shared.Enums.SetType.Working,
                        technique = (int)ShapeUp.Features.Training.Shared.Enums.Technique.Straight,
                        rpe = 8,
                        restSeconds = 90
                    }
                }
            }
        }
    };

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

    private async Task<string> CreateTemplateAsync(int exerciseId)
    {
        var response = await _client.PostAsJsonAsync("/api/training/workout-templates", new
        {
            name = $"Template A-{Guid.NewGuid():N}",
            notes = "notes",
            durationInWeeks = 4,
            phase = "Hypertrophy",
            difficulty = (int)ShapeUp.Features.Training.Shared.Enums.Difficulty.Intermediate,
            exercises = new[]
            {
                new
                {
                    exerciseId,
                    sets = new[]
                    {
                        new
                        {
                            repetitions = 12,
                            load = 15m,
                            loadUnit = (int)ShapeUp.Features.Training.Shared.Enums.LoadUnit.Kg,
                            setType = (int)ShapeUp.Features.Training.Shared.Enums.SetType.Working,
                            technique = (int)ShapeUp.Features.Training.Shared.Enums.Technique.Straight,
                            rpe = 7,
                            restSeconds = 60
                        }
                    }
                }
            }
        });

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<TemplatePayload>();
        return payload!.TemplateId;
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
        if (scopes.Length > 0)
            await TestDataSeeder.AssignScopesToUserAsync(context, user.Id, scopes);

        return (user.Id, TestFirebaseService.CreateToken(user.FirebaseUid, user.Email));
    }

    private sealed record TemplatePayload(string TemplateId);
    private sealed record EquipmentPayload(int Id, string Name, string NamePt, string? Description);
    private sealed record ExercisePayload(int Id, string Name, string NamePt);
}
