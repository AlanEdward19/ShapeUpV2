namespace IntegrationTests.Domains.Training.Endpoints;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Infrastructure;
using ShapeUp.Features.Relationships.Infrastructure.Repositories;
using ShapeUp.Features.Relationships.Shared.Entities;

/// <summary>
/// Covers WorkoutPlansController after RequireScopesAttribute removal (native-authorization-model
/// Phase 3, T19): authentication alone is the gate at the controller boundary now, fine-grained
/// authorization runs inside the handlers via ITrainingAccessPolicy / inline ownership checks.
/// These tests confirm removing the attribute did not open a gap.
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
        var created = await response.Content.ReadFromJsonAsync<WorkoutPlanPayload>();
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

    // Equipments/Exercises controllers still gate on RequireScopesAttribute (separate migration task),
    // so the acting user needs those scopes to set up fixtures for these WorkoutPlans tests.
    private async Task<ExercisePayload> CreateExerciseAsync(AuthorizedUser actor)
    {
        await using (var context = fixture.CreateAuthorizationDbContext())
        {
            await TestDataSeeder.AssignScopesToUserAsync(context, actor.UserId,
                "training:equipments:create", "training:exercises:create");
        }

        // native-authorization-model: Exercises/Equipments Create now require PlatformRoleType.Admin
        // instead of a Scope. This helper only seeds fixture data (an exercise to build a workout
        // plan around), never tests the catalog endpoints' own authorization boundary.
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
            muscles = new[] { new { muscleGroup = (int)ShapeUp.Features.Training.Shared.Enums.MuscleGroup.Chest, activationPercent = 70m } },
            equipmentIds = new[] { equipmentPayload.Id },
            steps = new[] { new { description = "Brace and press" } }
        });
        exercise.EnsureSuccessStatusCode();
        return (await exercise.Content.ReadFromJsonAsync<ExercisePayload>())!;
    }

    private static object BuildPlanBody(int exerciseId, int? targetUserId = null)
    {
        var exercises = new[]
        {
            new
            {
                exerciseId,
                sets = new[]
                {
                    new
                    {
                        repetitions = 8,
                        load = 100m,
                        loadUnit = (int)ShapeUp.Features.Training.Shared.Enums.LoadUnit.Kg,
                        setType = (int)ShapeUp.Features.Training.Shared.Enums.SetType.Working,
                        technique = (int)ShapeUp.Features.Training.Shared.Enums.Technique.Straight,
                        rpe = 8,
                        restSeconds = 90
                    }
                }
            }
        };

        return targetUserId is null
            ? new
            {
                name = $"Plan-{Guid.NewGuid():N}",
                notes = "Integration test plan",
                durationInWeeks = 4,
                phase = "Hypertrophy",
                difficulty = (int)ShapeUp.Features.Training.Shared.Enums.Difficulty.Intermediate,
                exercises
            }
            : new
            {
                targetUserId = targetUserId.Value,
                name = $"Plan-{Guid.NewGuid():N}",
                notes = "Integration test plan",
                durationInWeeks = 4,
                phase = "Hypertrophy",
                difficulty = (int)ShapeUp.Features.Training.Shared.Enums.Difficulty.Intermediate,
                exercises
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
        return (await response.Content.ReadFromJsonAsync<WorkoutPlanPayload>())!;
    }

    private sealed record AuthorizedUser(int UserId, string Token);
    private sealed record EquipmentPayload(int Id, string Name, string NamePt, string? Description);
    private sealed record ExercisePayload(int Id, string Name, string NamePt, object[] Muscles, object[] Equipments, string[] Steps);
    private sealed record WorkoutPlanPayload(string PlanId, int TargetUserId, string Name);
}
