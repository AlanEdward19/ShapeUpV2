namespace IntegrationTests.Domains.GymManagement.Endpoints;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using IntegrationTests.Infrastructure;

[Collection("Integration SQL Server")]
public sealed class GymManagementEndpointsIntegrationTests(SqlServerFixture fixture) : IAsyncLifetime
{
    private IntegrationWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await fixture.ResetDatabaseAsync(CancellationToken.None);
        _factory = new IntegrationWebApplicationFactory(fixture);
        _client = _factory.CreateClient();

        var token = TestFirebaseService.CreateToken($"gym-owner-{Guid.NewGuid():N}", $"owner-{Guid.NewGuid():N}@test.local");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Theory]
    [InlineData("Starter", 29.90)]
    [InlineData("Pro", 79.90)]
    public async Task PlatformTierEndpoints_ShouldCreateAndList(string name, decimal price)
    {
        var create = await _client.PostAsJsonAsync("/api/gym-management/platform-tiers", new
        {
            name = $"{name}-{Guid.NewGuid():N}",
            description = "desc",
            price,
            maxClients = (int?)null,
            maxTrainers = (int?)null
        });

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var list = await _client.GetAsync("/api/gym-management/platform-tiers?pageSize=5");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
    }

    [Theory]
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

    [Theory]
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

    [Theory]
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

    [Theory]
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


