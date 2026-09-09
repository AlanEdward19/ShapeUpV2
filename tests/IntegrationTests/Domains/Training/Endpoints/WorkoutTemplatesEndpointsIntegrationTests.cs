namespace IntegrationTests.Domains.Training.Endpoints;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Infrastructure;
using ShapeUp.Features.Training.Shared.Enums;

/// <summary>
/// Also covers the workout-editor feature's Block model (Straight/Superset/Amrap/Emom) and
/// exclusive RPE/RIR intensity (WOED-01..08) - T14b.
/// </summary>
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

    // --- workout-editor: Block model (WOED-01..08) ---

    [Fact]
    public async Task Create_SupersetWithTwoExercises_ReturnsCreatedWithBlockPersisted()
    {
        var owner = await SeedUserAsync("training:equipments:create", "training:exercises:create");
        Authorize(owner.Token);
        var exerciseA = await CreateExerciseAsync();
        var exerciseB = await CreateExerciseAsync();

        var body = BuildTemplateBodyWithBlocks(
            BuildBlockBody(BlockType.Superset, [
                BuildExercise(exerciseA, BuildSet(restSeconds: null)),
                BuildExercise(exerciseB, BuildSet(restSeconds: null))
            ]));

        var response = await _client.PostAsJsonAsync("/api/training/workout-templates", body);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = (await response.Content.ReadFromJsonAsync<WorkoutTemplatePayload>(JsonOptions))!;
        Assert.Single(created.Blocks);
        Assert.Equal(BlockType.Superset, created.Blocks[0].Type);
        Assert.Equal(2, created.Blocks[0].Exercises.Length);
    }

    [Fact]
    public async Task Create_SupersetWithOneExercise_ReturnsBadRequest()
    {
        var owner = await SeedUserAsync("training:equipments:create", "training:exercises:create");
        Authorize(owner.Token);
        var exercise = await CreateExerciseAsync();

        var body = BuildTemplateBodyWithBlocks(
            BuildBlockBody(BlockType.Superset, [BuildExercise(exercise, BuildSet(restSeconds: null))]));

        var response = await _client.PostAsJsonAsync("/api/training/workout-templates", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_AmrapMissingTimeCap_ReturnsBadRequest()
    {
        var owner = await SeedUserAsync("training:equipments:create", "training:exercises:create");
        Authorize(owner.Token);
        var exercise = await CreateExerciseAsync();

        var body = BuildTemplateBodyWithBlocks(
            BuildBlockBody(BlockType.Amrap, [BuildExercise(exercise, BuildSet(restSeconds: null))]));

        var response = await _client.PostAsJsonAsync("/api/training/workout-templates", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_AmrapValidWithAndWithoutFixedReps_ReturnsCreated()
    {
        var owner = await SeedUserAsync("training:equipments:create", "training:exercises:create");
        Authorize(owner.Token);
        var exerciseA = await CreateExerciseAsync();
        var exerciseB = await CreateExerciseAsync();

        var body = BuildTemplateBodyWithBlocks(
            BuildBlockBody(BlockType.Amrap, [
                BuildExercise(exerciseA, BuildSet(repetitions: null, restSeconds: null)),
                BuildExercise(exerciseB, BuildSet(repetitions: 12, restSeconds: null))
            ], timeCapSeconds: 600));

        var response = await _client.PostAsJsonAsync("/api/training/workout-templates", body);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = (await response.Content.ReadFromJsonAsync<WorkoutTemplatePayload>(JsonOptions))!;
        Assert.Equal(600, created.Blocks[0].TimeCapSeconds);
        Assert.Null(created.Blocks[0].Exercises[0].Sets[0].Repetitions);
        Assert.Equal(12, created.Blocks[0].Exercises[1].Sets[0].Repetitions);
    }

    [Fact]
    public async Task Create_EmomMissingIntervalOrRounds_ReturnsBadRequest()
    {
        var owner = await SeedUserAsync("training:equipments:create", "training:exercises:create");
        Authorize(owner.Token);
        var exercise = await CreateExerciseAsync();

        var body = BuildTemplateBodyWithBlocks(
            BuildBlockBody(BlockType.Emom, [BuildExercise(exercise, BuildSet(restSeconds: null))]));

        var response = await _client.PostAsJsonAsync("/api/training/workout-templates", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_EmomValidWithTwoExerciseRotation_ReturnsCreatedOrderPreserved()
    {
        var owner = await SeedUserAsync("training:equipments:create", "training:exercises:create");
        Authorize(owner.Token);
        var exerciseA = await CreateExerciseAsync();
        var exerciseB = await CreateExerciseAsync();

        var body = BuildTemplateBodyWithBlocks(
            BuildBlockBody(BlockType.Emom, [
                BuildExercise(exerciseA, BuildSet(restSeconds: null)),
                BuildExercise(exerciseB, BuildSet(restSeconds: null))
            ], intervalSeconds: 60, totalRounds: 10));

        var response = await _client.PostAsJsonAsync("/api/training/workout-templates", body);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = (await response.Content.ReadFromJsonAsync<WorkoutTemplatePayload>(JsonOptions))!;
        Assert.Equal(60, created.Blocks[0].IntervalSeconds);
        Assert.Equal(10, created.Blocks[0].TotalRounds);
        Assert.Equal(exerciseA, created.Blocks[0].Exercises[0].ExerciseId);
        Assert.Equal(exerciseB, created.Blocks[0].Exercises[1].ExerciseId);
    }

    [Fact]
    public async Task Create_RestSecondsOnNonStraightBlock_ReturnsBadRequest()
    {
        var owner = await SeedUserAsync("training:equipments:create", "training:exercises:create");
        Authorize(owner.Token);
        var exerciseA = await CreateExerciseAsync();
        var exerciseB = await CreateExerciseAsync();

        var body = BuildTemplateBodyWithBlocks(
            BuildBlockBody(BlockType.Superset, [
                BuildExercise(exerciseA, BuildSet(restSeconds: 90)),
                BuildExercise(exerciseB, BuildSet(restSeconds: null))
            ]));

        var response = await _client.PostAsJsonAsync("/api/training/workout-templates", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_IntensityOmitted_ReturnsCreated()
    {
        var owner = await SeedUserAsync("training:equipments:create", "training:exercises:create");
        Authorize(owner.Token);
        var exercise = await CreateExerciseAsync();

        var body = BuildTemplateBodyWithBlocks(
            BuildBlockBody(BlockType.Straight, [BuildExercise(exercise, BuildSet(intensity: false))]));

        var response = await _client.PostAsJsonAsync("/api/training/workout-templates", body);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = (await response.Content.ReadFromJsonAsync<WorkoutTemplatePayload>(JsonOptions))!;
        Assert.Null(created.Blocks[0].Exercises[0].Sets[0].Intensity);
    }

    private static object BuildSet(int? repetitions = 8, decimal load = 100m, int? restSeconds = 90, bool intensity = true) => new
    {
        repetitions,
        load,
        loadUnit = (int)LoadUnit.Kg,
        setType = (int)SetType.Working,
        technique = (int)Technique.Straight,
        intensity = intensity ? new { type = (int)IntensityType.Rpe, value = 8 } : null,
        restSeconds
    };

    private static object BuildExercise(int exerciseId, params object[] sets) => new { exerciseId, sets };

    private static object BuildBlockBody(
        BlockType type,
        object[] exercises,
        int? timeCapSeconds = null,
        int? intervalSeconds = null,
        int? totalRounds = null,
        int? restAfterSeconds = null) => new
        {
            type = (int)type,
            exercises,
            timeCapSeconds,
            intervalSeconds,
            totalRounds,
            restAfterSeconds
        };

    private static object BuildTemplateBodyWithBlocks(params object[] blocks) => new
    {
        name = $"Template A-{Guid.NewGuid():N}",
        notes = "notes",
        durationInWeeks = 4,
        phase = "Hypertrophy",
        difficulty = (int)Difficulty.Intermediate,
        blocks
    };

    private static object UpdateBody(int exerciseId) => new
    {
        name = $"Template Updated-{Guid.NewGuid():N}",
        notes = "updated notes",
        durationInWeeks = 6,
        phase = "Strength",
        difficulty = (int)Difficulty.Advanced,
        blocks = new[]
        {
            BuildBlockBody(BlockType.Straight, [BuildExercise(exerciseId, BuildSet(load: 40m, restSeconds: 90))])
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
            muscles = new[] { new { muscleGroup = (int)MuscleGroup.Chest, activationPercent = 70m } },
            equipmentIds = new[] { equipmentId },
            steps = new[] { new { description = "Brace and press" } }
        });
        exercise.EnsureSuccessStatusCode();
        return (await exercise.Content.ReadFromJsonAsync<ExercisePayload>())!.Id;
    }

    private async Task<string> CreateTemplateAsync(int exerciseId)
    {
        var body = BuildTemplateBodyWithBlocks(
            BuildBlockBody(BlockType.Straight, [BuildExercise(exerciseId, BuildSet(repetitions: 12, load: 15m, restSeconds: 60, intensity: true))]));

        var response = await _client.PostAsJsonAsync("/api/training/workout-templates", body);

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

        // native-authorization-model: Exercises/Equipments Create now require PlatformRoleType.Admin
        // instead of a Scope. This helper only ever seeds fixture data, never tests the catalog
        // endpoints' own authorization boundary.
        if (scopes.Contains("training:exercises:create") || scopes.Contains("training:equipments:create"))
        {
            await using var gymContext = fixture.CreateGymManagementDbContext();
            await TestDataSeeder.GrantPlatformAdminAsync(gymContext, user.Id, CancellationToken.None);
        }

        return (user.Id, TestFirebaseService.CreateToken(user.FirebaseUid, user.Email));
    }

    // API serializes enums as camelCase strings (DependencyInjectionExtensions.cs adds a
    // JsonStringEnumConverter globally) - the default ReadFromJsonAsync options don't know that,
    // so payload records with enum fields need these options passed explicitly.
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: true) }
    };

    private sealed record TemplatePayload(string TemplateId);
    private sealed record EquipmentPayload(int Id, string Name, string NamePt, string? Description);
    private sealed record ExercisePayload(int Id, string Name, string NamePt);
    private sealed record WorkoutTemplatePayload(string TemplateId, BlockPayload[] Blocks);
    private sealed record BlockPayload(BlockType Type, ExerciseInBlockPayload[] Exercises, int? TimeCapSeconds, int? IntervalSeconds, int? TotalRounds, int? RestAfterSeconds);
    private sealed record ExerciseInBlockPayload(int ExerciseId, SetPayload[] Sets, double? StrengthGainPercentage);
    private sealed record SetPayload(int? Repetitions, decimal Load, LoadUnit LoadUnit, SetType SetType, Technique Technique, IntensityPayload? Intensity, int? RestSeconds);
    private sealed record IntensityPayload(IntensityType Type, int Value);
}
