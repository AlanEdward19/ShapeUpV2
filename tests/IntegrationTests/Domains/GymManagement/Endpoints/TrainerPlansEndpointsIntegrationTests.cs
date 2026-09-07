using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using IntegrationTests.Infrastructure;

namespace IntegrationTests.Domains.GymManagement.Endpoints;

/// <summary>
/// Covers T12 (native-authorization-model): TrainerPlansController migrated from
/// RequireScopesAttribute to [Authorize(Policy = "capability:gym.trainer_plans.*")]. This route
/// has no {gymId} segment, so CapabilityResolver only grants access via self-access
/// (TargetUserId == current userId, extracted from the {trainerId} route value) -- there is no
/// membership/gym-staff source for this controller today (see SPEC_DEVIATION in the task report).
/// </summary>
[Collection("SQL Server Write Operations")]
public sealed class TrainerPlansEndpointsIntegrationTests(SqlServerFixture fixture) : IAsyncLifetime
{
    private IntegrationWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
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
    public async Task GetAll_ShouldReturnOk_WhenTrainerRequestsOwnPlans()
    {
        var trainer = await SeedTrainerTokenAsync("get-all-self");

        var response = await _client.GetAsync($"/api/gym-management/trainers/{trainer.Id}/plans");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_ShouldReturnForbidden_WhenTrainerIdIsNotCurrentUser()
    {
        var trainer = await SeedTrainerTokenAsync("get-all-other");
        var otherTrainerId = trainer.Id + 987654;

        var response = await _client.GetAsync($"/api/gym-management/trainers/{otherTrainerId}/plans");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Create_ShouldReturnCreated_WhenTrainerCreatesOwnPlan()
    {
        var trainer = await SeedTrainerTokenAsync("create-self");

        var response = await _client.PostAsJsonAsync($"/api/gym-management/trainers/{trainer.Id}/plans", new
        {
            name = $"Plan-{Guid.NewGuid():N}",
            description = "desc",
            price = 49.90,
            durationDays = 30
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Create_ShouldReturnForbidden_WhenTrainerIdIsNotCurrentUser()
    {
        var trainer = await SeedTrainerTokenAsync("create-other");
        var otherTrainerId = trainer.Id + 987654;

        var response = await _client.PostAsJsonAsync($"/api/gym-management/trainers/{otherTrainerId}/plans", new
        {
            name = $"Plan-{Guid.NewGuid():N}",
            description = "desc",
            price = 49.90,
            durationDays = 30
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Update_ShouldReturnOk_WhenTrainerUpdatesOwnPlan()
    {
        var trainer = await SeedTrainerTokenAsync("update-self");
        var planId = await CreatePlanAsync(trainer.Id);

        var response = await _client.PutAsJsonAsync($"/api/gym-management/trainers/{trainer.Id}/plans/{planId}", new
        {
            planId,
            name = "Updated Plan",
            description = "updated",
            price = 59.90,
            durationDays = 60,
            isActive = true
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Update_ShouldReturnForbidden_WhenTrainerIdIsNotCurrentUser()
    {
        var trainer = await SeedTrainerTokenAsync("update-other");
        var planId = await CreatePlanAsync(trainer.Id);
        var otherTrainerId = trainer.Id + 987654;

        var response = await _client.PutAsJsonAsync($"/api/gym-management/trainers/{otherTrainerId}/plans/{planId}", new
        {
            planId,
            name = "Updated Plan",
            description = "updated",
            price = 59.90,
            durationDays = 60,
            isActive = true
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ShouldReturnOk_WhenTrainerDeletesOwnPlan()
    {
        var trainer = await SeedTrainerTokenAsync("delete-self");
        var planId = await CreatePlanAsync(trainer.Id);

        var response = await _client.DeleteAsync($"/api/gym-management/trainers/{trainer.Id}/plans/{planId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ShouldReturnForbidden_WhenTrainerIdIsNotCurrentUser()
    {
        var trainer = await SeedTrainerTokenAsync("delete-other");
        var planId = await CreatePlanAsync(trainer.Id);
        var otherTrainerId = trainer.Id + 987654;

        var response = await _client.DeleteAsync($"/api/gym-management/trainers/{otherTrainerId}/plans/{planId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<int> CreatePlanAsync(int trainerId)
    {
        var response = await _client.PostAsJsonAsync($"/api/gym-management/trainers/{trainerId}/plans", new
        {
            name = $"Plan-{Guid.NewGuid():N}",
            description = "desc",
            price = 49.90,
            durationDays = 30
        });

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<CreatedTrainerPlanPayload>();
        return payload!.Id;
    }

    private async Task<ShapeUp.Features.Authorization.Shared.Entities.User> SeedTrainerTokenAsync(string suffix)
    {
        await using var context = fixture.CreateAuthorizationDbContext();
        var user = await TestDataSeeder.SeedUserAsync(context, $"trainer-plans-{suffix}-{Guid.NewGuid():N}", CancellationToken.None);

        var token = TestFirebaseService.CreateToken(user.FirebaseUid, user.Email);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return user;
    }

    private sealed record CreatedTrainerPlanPayload(int Id, int TrainerId, string Name);
}
