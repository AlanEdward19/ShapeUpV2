namespace IntegrationTests.Domains.Training.Endpoints;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Infrastructure;
using ShapeUp.Features.Relationships.Infrastructure.Repositories;
using ShapeUp.Features.Relationships.Shared.Entities;
using ShapeUp.Features.Training.Shared.Enums;

/// <summary>
/// Covers WorkoutPlansController after RequireScopesAttribute removal (native-authorization-model
/// Phase 3, T19): authentication alone is the gate at the controller boundary now, fine-grained
/// authorization runs inside the handlers via ITrainingAccessPolicy / inline ownership checks.
/// These tests confirm removing the attribute did not open a gap.
///
/// Also covers the workout-editor feature's Block model (Straight/Superset/Amrap/Emom) and
/// exclusive RPE/RIR intensity (WOED-01..08) - T14.
/// </summary>
[Collection("SQL Server Write Operations")]
public sealed class WorkoutPlansEndpointsIntegrationTests(SqlServerFixture fixture) : IAsyncLifetime
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
    public async Task Create_OwnPlan_ReturnsCreated()
    {
        var actor = await SeedUserAsync();
        var exercise = await CreateExerciseAsync(actor);

        var response = await CreatePlanAsync(actor, actor.UserId, exercise.Id);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<WorkoutPlanPayload>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal(actor.UserId, created!.TargetUserId);
    }

    [Fact]
    public async Task Create_ForTargetUserWithoutRelationship_ReturnsForbidden()
    {
        var actor = await SeedUserAsync();
        var target = await SeedUserAsync();
        var exercise = await CreateExerciseAsync(actor);

        var response = await CreatePlanAsync(actor, target.UserId, exercise.Id);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Copy_OwnPlanForSelf_ReturnsCreated()
    {
        var actor = await SeedUserAsync();
        var exercise = await CreateExerciseAsync(actor);
        var plan = await CreateAndReadPlanAsync(actor, actor.UserId, exercise.Id);

        Authorize(actor.Token);
        var response = await _client.PostAsJsonAsync($"/api/training/workout-plans/{plan.PlanId}/copy", new { });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Copy_ToTargetUserWithoutRelationship_ReturnsForbidden()
    {
        var actor = await SeedUserAsync();
        var target = await SeedUserAsync();
        var exercise = await CreateExerciseAsync(actor);
        var plan = await CreateAndReadPlanAsync(actor, actor.UserId, exercise.Id);

        Authorize(actor.Token);
        var response = await _client.PostAsJsonAsync($"/api/training/workout-plans/{plan.PlanId}/copy", new { targetUserId = target.UserId });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetById_Owner_ReturnsOk()
    {
        var actor = await SeedUserAsync();
        var exercise = await CreateExerciseAsync(actor);
        var plan = await CreateAndReadPlanAsync(actor, actor.UserId, exercise.Id);

        Authorize(actor.Token);
        var response = await _client.GetAsync($"/api/training/workout-plans/{plan.PlanId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetById_UnrelatedUser_ReturnsForbidden()
    {
        var owner = await SeedUserAsync();
        var exercise = await CreateExerciseAsync(owner);
        var plan = await CreateAndReadPlanAsync(owner, owner.UserId, exercise.Id);

        var stranger = await SeedUserAsync();
        Authorize(stranger.Token);
        var response = await _client.GetAsync($"/api/training/workout-plans/{plan.PlanId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetByUser_Self_ReturnsOk()
    {
        var actor = await SeedUserAsync();
        Authorize(actor.Token);

        var response = await _client.GetAsync($"/api/training/workout-plans/user/{actor.UserId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetByUser_UnrelatedTarget_ReturnsForbidden()
    {
        var actor = await SeedUserAsync();
        var target = await SeedUserAsync();
        Authorize(actor.Token);

        var response = await _client.GetAsync($"/api/training/workout-plans/user/{target.UserId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetByUser_ActorWithActiveRelationshipToTarget_ReturnsOk()
    {
        var actor = await SeedUserAsync();
        var target = await SeedUserAsync();
        await SeedActiveRelationshipAsync(actor.UserId, target.UserId);
        Authorize(actor.Token);

        var response = await _client.GetAsync($"/api/training/workout-plans/user/{target.UserId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Update_Owner_ReturnsOk()
    {
        var actor = await SeedUserAsync();
        var exercise = await CreateExerciseAsync(actor);
        var plan = await CreateAndReadPlanAsync(actor, actor.UserId, exercise.Id);

        Authorize(actor.Token);
        var response = await _client.PutAsJsonAsync($"/api/training/workout-plans/{plan.PlanId}", BuildPlanBody(exercise.Id));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Update_NonOwner_ReturnsForbidden()
    {
        var owner = await SeedUserAsync();
        var exercise = await CreateExerciseAsync(owner);
        var plan = await CreateAndReadPlanAsync(owner, owner.UserId, exercise.Id);

        var stranger = await SeedUserAsync();
        Authorize(stranger.Token);
        var response = await _client.PutAsJsonAsync($"/api/training/workout-plans/{plan.PlanId}", BuildPlanBody(exercise.Id));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Owner_ReturnsOk()
    {
        var actor = await SeedUserAsync();
        var exercise = await CreateExerciseAsync(actor);
        var plan = await CreateAndReadPlanAsync(actor, actor.UserId, exercise.Id);

        Authorize(actor.Token);
        var response = await _client.DeleteAsync($"/api/training/workout-plans/{plan.PlanId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Delete_NonOwner_ReturnsForbidden()
    {
        var owner = await SeedUserAsync();
        var exercise = await CreateExerciseAsync(owner);
        var plan = await CreateAndReadPlanAsync(owner, owner.UserId, exercise.Id);

        var stranger = await SeedUserAsync();
        Authorize(stranger.Token);
        var response = await _client.DeleteAsync($"/api/training/workout-plans/{plan.PlanId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // --- workout-editor: Block model (WOED-01..08) ---

    [Fact]
    public async Task Create_SupersetWithTwoExercises_ReturnsCreatedWithBlockPersisted()
    {
        var actor = await SeedUserAsync();
        var exerciseA = await CreateExerciseAsync(actor);
        var exerciseB = await CreateExerciseAsync(actor);
        Authorize(actor.Token);

        var body = BuildPlanBodyWithBlocks(actor.UserId,
            BuildBlockBody(BlockType.Superset, [
                BuildExercise(exerciseA.Id, BuildSet(restSeconds: null)),
                BuildExercise(exerciseB.Id, BuildSet(restSeconds: null))
            ]));

        var response = await _client.PostAsJsonAsync("/api/training/workout-plans", body);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = (await response.Content.ReadFromJsonAsync<WorkoutPlanPayload>(JsonOptions))!;
        Assert.Single(created.Blocks);
        Assert.Equal(BlockType.Superset, created.Blocks[0].Type);
        Assert.Equal(2, created.Blocks[0].Exercises.Length);
    }

    [Fact]
    public async Task Create_SupersetWithOneExercise_ReturnsBadRequest()
    {
        var actor = await SeedUserAsync();
        var exercise = await CreateExerciseAsync(actor);
        Authorize(actor.Token);

        var body = BuildPlanBodyWithBlocks(actor.UserId,
            BuildBlockBody(BlockType.Superset, [BuildExercise(exercise.Id, BuildSet(restSeconds: null))]));

        var response = await _client.PostAsJsonAsync("/api/training/workout-plans", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_AmrapMissingTimeCap_ReturnsBadRequest()
    {
        var actor = await SeedUserAsync();
        var exercise = await CreateExerciseAsync(actor);
        Authorize(actor.Token);

        var body = BuildPlanBodyWithBlocks(actor.UserId,
            BuildBlockBody(BlockType.Amrap, [BuildExercise(exercise.Id, BuildSet(restSeconds: null))]));

        var response = await _client.PostAsJsonAsync("/api/training/workout-plans", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_AmrapValidWithAndWithoutFixedReps_ReturnsCreated()
    {
        var actor = await SeedUserAsync();
        var exerciseA = await CreateExerciseAsync(actor);
        var exerciseB = await CreateExerciseAsync(actor);
        Authorize(actor.Token);

        var body = BuildPlanBodyWithBlocks(actor.UserId,
            BuildBlockBody(BlockType.Amrap, [
                BuildExercise(exerciseA.Id, BuildSet(repetitions: null, restSeconds: null)),
                BuildExercise(exerciseB.Id, BuildSet(repetitions: 12, restSeconds: null))
            ], timeCapSeconds: 600));

        var response = await _client.PostAsJsonAsync("/api/training/workout-plans", body);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = (await response.Content.ReadFromJsonAsync<WorkoutPlanPayload>(JsonOptions))!;
        Assert.Equal(600, created.Blocks[0].TimeCapSeconds);
        Assert.Null(created.Blocks[0].Exercises[0].Sets[0].Repetitions);
        Assert.Equal(12, created.Blocks[0].Exercises[1].Sets[0].Repetitions);
    }

    [Fact]
    public async Task Create_EmomMissingIntervalOrRounds_ReturnsBadRequest()
    {
        var actor = await SeedUserAsync();
        var exercise = await CreateExerciseAsync(actor);
        Authorize(actor.Token);

        var body = BuildPlanBodyWithBlocks(actor.UserId,
            BuildBlockBody(BlockType.Emom, [BuildExercise(exercise.Id, BuildSet(restSeconds: null))]));

        var response = await _client.PostAsJsonAsync("/api/training/workout-plans", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_EmomValidWithTwoExerciseRotation_ReturnsCreatedOrderPreserved()
    {
        var actor = await SeedUserAsync();
        var exerciseA = await CreateExerciseAsync(actor);
        var exerciseB = await CreateExerciseAsync(actor);
        Authorize(actor.Token);

        var body = BuildPlanBodyWithBlocks(actor.UserId,
            BuildBlockBody(BlockType.Emom, [
                BuildExercise(exerciseA.Id, BuildSet(restSeconds: null)),
                BuildExercise(exerciseB.Id, BuildSet(restSeconds: null))
            ], intervalSeconds: 60, totalRounds: 10));

        var response = await _client.PostAsJsonAsync("/api/training/workout-plans", body);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = (await response.Content.ReadFromJsonAsync<WorkoutPlanPayload>(JsonOptions))!;
        Assert.Equal(60, created.Blocks[0].IntervalSeconds);
        Assert.Equal(10, created.Blocks[0].TotalRounds);
        Assert.Equal(exerciseA.Id, created.Blocks[0].Exercises[0].ExerciseId);
        Assert.Equal(exerciseB.Id, created.Blocks[0].Exercises[1].ExerciseId);
    }

    [Fact]
    public async Task Create_RestSecondsOnNonStraightBlock_ReturnsBadRequest()
    {
        var actor = await SeedUserAsync();
        var exerciseA = await CreateExerciseAsync(actor);
        var exerciseB = await CreateExerciseAsync(actor);
        Authorize(actor.Token);

        var body = BuildPlanBodyWithBlocks(actor.UserId,
            BuildBlockBody(BlockType.Superset, [
                BuildExercise(exerciseA.Id, BuildSet(restSeconds: 90)),
                BuildExercise(exerciseB.Id, BuildSet(restSeconds: null))
            ]));

        var response = await _client.PostAsJsonAsync("/api/training/workout-plans", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_IntensityOmitted_ReturnsCreated()
    {
        var actor = await SeedUserAsync();
        var exercise = await CreateExerciseAsync(actor);
        Authorize(actor.Token);

        var body = BuildPlanBodyWithBlocks(actor.UserId,
            BuildBlockBody(BlockType.Straight, [BuildExercise(exercise.Id, BuildSet(intensity: false))]));

        var response = await _client.PostAsJsonAsync("/api/training/workout-plans", body);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = (await response.Content.ReadFromJsonAsync<WorkoutPlanPayload>(JsonOptions))!;
        Assert.Null(created.Blocks[0].Exercises[0].Sets[0].Intensity);
    }

    // API serializes enums as camelCase strings (DependencyInjectionExtensions.cs adds a
    // JsonStringEnumConverter globally) - the default ReadFromJsonAsync options don't know that,
    // so payload records with enum fields need these options passed explicitly.
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: true) }
    };

    private void Authorize(string token)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private async Task<AuthorizedUser> SeedUserAsync()
    {
        await using var context = fixture.CreateAuthorizationDbContext();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var user = await TestDataSeeder.SeedUserAsync(context, suffix, CancellationToken.None);
        return new AuthorizedUser(user.Id, TestFirebaseService.CreateToken(user.FirebaseUid, user.Email));
    }

    private async Task SeedActiveRelationshipAsync(int professionalUserId, int clientUserId)
    {
        await using var context = fixture.CreateRelationshipsDbContext();
        var repository = new ProfessionalClientRelationshipRepository(context);
        var result = await repository.CreateAsync(new ProfessionalClientRelationship
        {
            ProfessionalUserId = professionalUserId,
            ClientUserId = clientUserId,
            RelationshipType = "Training",
            Status = RelationshipStatus.Active,
            StartedAt = DateTime.UtcNow
        }, CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    // Exercises/Equipments Create require PlatformRoleType.Admin. This helper only seeds fixture
    // data (an exercise to build a workout plan around), never tests the catalog endpoints' own
    // authorization boundary.
    private async Task<ExercisePayload> CreateExerciseAsync(AuthorizedUser actor)
    {
        await using (var gymContext = fixture.CreateGymManagementDbContext())
        {
            await TestDataSeeder.GrantPlatformAdminAsync(gymContext, actor.UserId, CancellationToken.None);
        }

        Authorize(actor.Token);

        var equipment = await _client.PostAsJsonAsync("/api/training/equipments", new
        {
            name = $"Barbell-{Guid.NewGuid():N}",
            namePt = $"Barra-{Guid.NewGuid():N}",
            description = "Olympic barbell"
        });
        equipment.EnsureSuccessStatusCode();
        var equipmentPayload = (await equipment.Content.ReadFromJsonAsync<EquipmentPayload>())!;

        var exercise = await _client.PostAsJsonAsync("/api/training/exercises", new
        {
            name = $"Bench Press-{Guid.NewGuid():N}",
            namePt = $"Supino-{Guid.NewGuid():N}",
            description = "Compound press",
            videoUrl = (string?)null,
            muscles = new[] { new { muscleGroup = (int)MuscleGroup.Chest, activationPercent = 70m } },
            equipmentIds = new[] { equipmentPayload.Id },
            steps = new[] { new { description = "Brace and press" } }
        });
        exercise.EnsureSuccessStatusCode();
        return (await exercise.Content.ReadFromJsonAsync<ExercisePayload>())!;
    }

    private static object BuildSet(int? repetitions = 8, decimal load = 100m, int? restSeconds = 90, bool intensity = true)
    {
        return new
        {
            repetitions,
            load,
            loadUnit = (int)LoadUnit.Kg,
            setType = (int)SetType.Working,
            technique = (int)Technique.Straight,
            intensity = intensity ? new { type = (int)IntensityType.Rpe, value = 8 } : null,
            restSeconds
        };
    }

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

    private static object BuildPlanBody(int exerciseId, int? targetUserId = null) =>
        BuildPlanBodyWithBlocks(targetUserId, BuildBlockBody(BlockType.Straight, [BuildExercise(exerciseId, BuildSet())]));

    private static object BuildPlanBodyWithBlocks(int? targetUserId, params object[] blocks)
    {
        return targetUserId is null
            ? new
            {
                name = $"Plan-{Guid.NewGuid():N}",
                notes = "Integration test plan",
                durationInWeeks = 4,
                phase = "Hypertrophy",
                difficulty = (int)Difficulty.Intermediate,
                blocks
            }
            : new
            {
                targetUserId = targetUserId.Value,
                name = $"Plan-{Guid.NewGuid():N}",
                notes = "Integration test plan",
                durationInWeeks = 4,
                phase = "Hypertrophy",
                difficulty = (int)Difficulty.Intermediate,
                blocks
            };
    }

    private async Task<HttpResponseMessage> CreatePlanAsync(AuthorizedUser actor, int targetUserId, int exerciseId)
    {
        Authorize(actor.Token);
        return await _client.PostAsJsonAsync("/api/training/workout-plans", BuildPlanBody(exerciseId, targetUserId));
    }

    private async Task<WorkoutPlanPayload> CreateAndReadPlanAsync(AuthorizedUser actor, int targetUserId, int exerciseId)
    {
        var response = await CreatePlanAsync(actor, targetUserId, exerciseId);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<WorkoutPlanPayload>(JsonOptions))!;
    }

    private sealed record AuthorizedUser(int UserId, string Token);
    private sealed record EquipmentPayload(int Id, string Name, string NamePt, string? Description);
    private sealed record ExercisePayload(int Id, string Name, string NamePt, object[] Muscles, object[] Equipments, string[] Steps);
    private sealed record WorkoutPlanPayload(string PlanId, int TargetUserId, string Name, BlockPayload[] Blocks);
    private sealed record BlockPayload(BlockType Type, ExerciseInBlockPayload[] Exercises, int? TimeCapSeconds, int? IntervalSeconds, int? TotalRounds, int? RestAfterSeconds);
    private sealed record ExerciseInBlockPayload(int ExerciseId, SetPayload[] Sets, double? StrengthGainPercentage);
    private sealed record SetPayload(int? Repetitions, decimal Load, LoadUnit LoadUnit, SetType SetType, Technique Technique, IntensityPayload? Intensity, int? RestSeconds);
    private sealed record IntensityPayload(IntensityType Type, int Value);
}
