using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using IntegrationTests.Infrastructure;

namespace IntegrationTests.Domains.GymManagement.Endpoints;

/// <summary>
/// Covers T13: TrainerClientsController migrated from RequireScopesAttribute to
/// [Authorize(Policy = "capability:...")]. AUTHZ-01/AUTHZ-02: the policy grants access only via
/// self-access (route trainerId == authenticated user id) -- delegated/gym-staff access on behalf
/// of a trainer is a documented gap (SPEC_DEVIATION), not covered here.
/// </summary>
[Collection("SQL Server Write Operations")]
public sealed class TrainerClientsEndpointsIntegrationTests(SqlServerFixture fixture) : IAsyncLifetime
{
    private IntegrationWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _factory = new IntegrationWebApplicationFactory(fixture);
        _client = _factory.CreateClient();
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task GetAll_SelfAccess_ShouldReturnOk()
    {
        var trainerId = await SeedUserAndAuthenticateAsync("gettc-self");

        var response = await _client.GetAsync($"/api/gym-management/trainers/{trainerId}/clients");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_CrossUser_ShouldReturnForbidden()
    {
        await SeedUserAndAuthenticateAsync("gettc-caller");
        var otherTrainerId = await SeedOtherUserIdAsync("gettc-other");

        var response = await _client.GetAsync($"/api/gym-management/trainers/{otherTrainerId}/clients");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Add_SelfAccess_ShouldNotBeForbidden()
    {
        var trainerId = await SeedUserAndAuthenticateAsync("addtc-self");

        var response = await _client.PostAsJsonAsync($"/api/gym-management/trainers/{trainerId}/clients", new
        {
            clientId = Random.Shared.Next(500_000, 600_000),
            trainerPlanId = (int?)null
        });

        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Add_CrossUser_ShouldReturnForbidden()
    {
        await SeedUserAndAuthenticateAsync("addtc-caller");
        var otherTrainerId = await SeedOtherUserIdAsync("addtc-other");

        var response = await _client.PostAsJsonAsync($"/api/gym-management/trainers/{otherTrainerId}/clients", new
        {
            clientId = Random.Shared.Next(500_000, 600_000),
            trainerPlanId = (int?)null
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GenerateInvite_SelfAccess_ShouldNotBeForbidden()
    {
        var trainerId = await SeedUserAndAuthenticateAsync("invtc-self");

        var response = await _client.PostAsJsonAsync(
            $"/api/gym-management/trainers/{trainerId}/clients/invites/invitee@integration.test",
            new { trainerPlanId = (int?)null, expiresInHours = (int?)null });

        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GenerateInvite_CrossUser_ShouldReturnForbidden()
    {
        await SeedUserAndAuthenticateAsync("invtc-caller");
        var otherTrainerId = await SeedOtherUserIdAsync("invtc-other");

        var response = await _client.PostAsJsonAsync(
            $"/api/gym-management/trainers/{otherTrainerId}/clients/invites/invitee@integration.test",
            new { trainerPlanId = (int?)null, expiresInHours = (int?)null });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Transfer_SelfAccess_ShouldNotBeForbidden()
    {
        var trainerId = await SeedUserAndAuthenticateAsync("trftc-self");

        var response = await _client.PutAsJsonAsync(
            $"/api/gym-management/trainers/{trainerId}/clients/{Random.Shared.Next(500_000, 600_000)}/transfer",
            new { newTrainerId = trainerId + 1, newPlanId = 1 });

        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Transfer_CrossUser_ShouldReturnForbidden()
    {
        await SeedUserAndAuthenticateAsync("trftc-caller");
        var otherTrainerId = await SeedOtherUserIdAsync("trftc-other");

        var response = await _client.PutAsJsonAsync(
            $"/api/gym-management/trainers/{otherTrainerId}/clients/{Random.Shared.Next(500_000, 600_000)}/transfer",
            new { newTrainerId = otherTrainerId + 1, newPlanId = 1 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Unassign_SelfAccess_ShouldNotBeForbidden()
    {
        var trainerId = await SeedUserAndAuthenticateAsync("unatc-self");

        var response = await _client.DeleteAsync(
            $"/api/gym-management/trainers/{trainerId}/clients/{Random.Shared.Next(500_000, 600_000)}");

        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Unassign_CrossUser_ShouldReturnForbidden()
    {
        await SeedUserAndAuthenticateAsync("unatc-caller");
        var otherTrainerId = await SeedOtherUserIdAsync("unatc-other");

        var response = await _client.DeleteAsync(
            $"/api/gym-management/trainers/{otherTrainerId}/clients/{Random.Shared.Next(500_000, 600_000)}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SetPlanStatus_SelfAccess_ShouldNotBeForbidden()
    {
        var trainerId = await SeedUserAndAuthenticateAsync("plstc-self");

        var response = await _client.PatchAsJsonAsync(
            $"/api/gym-management/trainers/{trainerId}/clients/{Random.Shared.Next(500_000, 600_000)}/plan/status",
            new { isActive = false });

        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SetPlanStatus_CrossUser_ShouldReturnForbidden()
    {
        await SeedUserAndAuthenticateAsync("plstc-caller");
        var otherTrainerId = await SeedOtherUserIdAsync("plstc-other");

        var response = await _client.PatchAsJsonAsync(
            $"/api/gym-management/trainers/{otherTrainerId}/clients/{Random.Shared.Next(500_000, 600_000)}/plan/status",
            new { isActive = false });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// <summary>Seeds a user, authenticates the shared client as them, and returns their DB id (trainerId for self-access).</summary>
    private async Task<int> SeedUserAndAuthenticateAsync(string suffix)
    {
        await using var context = fixture.CreateAuthorizationDbContext();
        var user = await TestDataSeeder.SeedUserAsync(context, suffix, CancellationToken.None);

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestFirebaseService.CreateToken(user.FirebaseUid, user.Email));

        return user.Id;
    }

    /// <summary>Seeds a second, unrelated user and returns their DB id -- used as a trainerId the caller does not own.</summary>
    private async Task<int> SeedOtherUserIdAsync(string suffix)
    {
        await using var context = fixture.CreateAuthorizationDbContext();
        var user = await TestDataSeeder.SeedUserAsync(context, suffix, CancellationToken.None);
        return user.Id;
    }
}
