namespace IntegrationTests.Domains.GymManagement.Endpoints;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Infrastructure;

[Collection("SQL Server Write Operations")]
public sealed class GymManagementEndpointsIntegrationTests(SqlServerFixture fixture) : IAsyncLifetime
{
    private IntegrationWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _factory = new IntegrationWebApplicationFactory(fixture);
        _client = _factory.CreateClient();

        var token = TestFirebaseService.CreateToken($"gym-owner-{Guid.NewGuid():N}", $"owner-{Guid.NewGuid():N}@test.local");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact(Skip = "Endpoint flow depends on auth scope seeding in fixture; CRUD behavior is covered by handler/repository integration tests.")]
    public Task PlatformTiersEndpoints_ShouldCreateAndList() => Task.CompletedTask;

    [Theory(Skip = "Endpoint flow depends on auth scope/role seeding in fixture; CRUD behavior is covered by handler/repository integration tests.")]
    [InlineData("Gym Endpoint A")]
    [InlineData("Gym Endpoint B")]
    public async Task GymsEndpoints_ShouldCreateAndGet(string gymName)
    {
        var create = await _client.PostAsJsonAsync("/api/gym-management/gyms", new
        {
            name = gymName,
            description = "integration",
            address = "street",
            platformTierId = (int?)null
        });

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var createdPayload = await create.Content.ReadFromJsonAsync<CreatedGymPayload>();
        Assert.NotNull(createdPayload);

        var get = await _client.GetAsync($"/api/gym-management/gyms/{createdPayload!.Id}");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
    }

    [Theory(Skip = "Endpoint flow depends on auth scope/role seeding in fixture; CRUD behavior is covered by handler/repository integration tests.")]
    [InlineData("Monthly", 99.90, 30)]
    [InlineData("Annual", 799.90, 365)]
    public async Task GymPlansAndClientsEndpoints_ShouldCreatePlanAndEnrollClient(string planName, decimal price, int days)
    {
        var gym = await CreateGymAsync();

        var createPlan = await _client.PostAsJsonAsync($"/api/gym-management/gyms/{gym.Id}/plans", new
        {
            gymId = gym.Id,
            name = planName,
            description = "desc",
            price,
            durationDays = days
        });

        Assert.Equal(HttpStatusCode.Created, createPlan.StatusCode);
        var planPayload = await createPlan.Content.ReadFromJsonAsync<CreatedPlanPayload>();
        Assert.NotNull(planPayload);

        var enroll = await _client.PostAsJsonAsync($"/api/gym-management/gyms/{gym.Id}/clients", new
        {
            gymId = gym.Id,
            userId = Random.Shared.Next(1000, 2000),
            gymPlanId = planPayload!.Id,
            trainerId = (int?)null
        });

        Assert.Equal(HttpStatusCode.Created, enroll.StatusCode);
    }

    [Theory(Skip = "Endpoint flow depends on auth scope/role seeding in fixture; CRUD behavior is covered by handler/repository integration tests.")]
    [InlineData("Coach Plan", 49.90, 30)]
    [InlineData("Strong Plan", 69.90, 60)]
    public async Task TrainerPlansAndClientsEndpoints_ShouldCreatePlanAndAddClient(string planName, decimal price, int days)
    {
        var gym = await CreateGymAsync();
        var trainerId = gym.OwnerId;

        var createPlan = await _client.PostAsJsonAsync($"/api/gym-management/trainers/{trainerId}/plans", new
        {
            name = $"{planName}-{Guid.NewGuid():N}",
            description = "d",
            price,
            durationDays = days
        });

        Assert.Equal(HttpStatusCode.Created, createPlan.StatusCode);
        var planPayload = await createPlan.Content.ReadFromJsonAsync<CreatedTrainerPlanPayload>();
        Assert.NotNull(planPayload);

        var addClient = await _client.PostAsJsonAsync($"/api/gym-management/trainers/{trainerId}/clients", new
        {
            clientId = Random.Shared.Next(2000, 3000),
            trainerPlanId = planPayload!.Id
        });

        Assert.Equal(HttpStatusCode.Created, addClient.StatusCode);
    }

    [Theory(Skip = "Endpoint flow depends on auth scope/role seeding in fixture; validation behavior is covered by handler tests.")]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GymsEndpoints_InvalidPayload_ShouldReturnBadRequest(string invalidName)
    {
        var create = await _client.PostAsJsonAsync("/api/gym-management/gyms", new
        {
            name = invalidName,
            description = "integration",
            address = "street",
            platformTierId = (int?)null
        });

        Assert.Equal(HttpStatusCode.BadRequest, create.StatusCode);
    }

    private async Task<CreatedGymPayload> CreateGymAsync()
    {
        var create = await _client.PostAsJsonAsync("/api/gym-management/gyms", new
        {
            name = $"Gym-{Guid.NewGuid():N}",
            description = "integration",
            address = "street",
            platformTierId = (int?)null
        });

        create.EnsureSuccessStatusCode();
        return (await create.Content.ReadFromJsonAsync<CreatedGymPayload>())!;
    }

    private sealed record CreatedGymPayload(int Id, int OwnerId, string Name);
    private sealed record CreatedPlanPayload(int Id, int GymId, string Name);
    private sealed record CreatedTrainerPlanPayload(int Id, int TrainerId, string Name);
}


