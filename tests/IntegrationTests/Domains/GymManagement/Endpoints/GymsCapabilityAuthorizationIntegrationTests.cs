namespace IntegrationTests.Domains.GymManagement.Endpoints;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Infrastructure;
using ShapeUp.Features.GymManagement.Shared.Entities;

/// <summary>
/// Covers T8: GymsController's {gymId}-scoped endpoints (GetById, Update, Delete) migrated from
/// RequireScopesAttribute to [Authorize(Policy = "capability:gym.*")]. Access is now gated purely
/// by gym membership (owner or active staff), resolved by CapabilityResolver via
/// OrganizationMembershipAdapter -- there is no scope seeding involved for these three endpoints.
/// GetAll/Create stay out of scope here: they are not migrated (see SPEC_DEVIATION note on
/// GymsController.GetAll).
/// </summary>
[Collection("SQL Server Write Operations")]
public sealed class GymsCapabilityAuthorizationIntegrationTests(SqlServerFixture fixture) : IAsyncLifetime
{
    private IntegrationWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _factory = new IntegrationWebApplicationFactory(fixture);
        _client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task GetById_ShouldReturnOk_WhenCallerIsGymOwner()
    {
        var (ownerId, ownerToken) = await SeedUserAsync("gym-read-owner");
        var gym = await SeedGymAsync(ownerId, "Read Owner Gym");

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
        var response = await _client.GetAsync($"/api/gym-management/gyms/{gym.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetById_ShouldReturnForbidden_WhenCallerHasNoMembership()
    {
        var (ownerId, _) = await SeedUserAsync("gym-read-owner-2");
        var gym = await SeedGymAsync(ownerId, "Read Deny Gym");
        var (_, outsiderToken) = await SeedUserAsync("gym-read-outsider");

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", outsiderToken);
        var response = await _client.GetAsync($"/api/gym-management/gyms/{gym.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Update_ShouldReturnOk_WhenCallerIsGymOwner()
    {
        var (ownerId, ownerToken) = await SeedUserAsync("gym-update-owner");
        var gym = await SeedGymAsync(ownerId, "Update Owner Gym");

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
        var response = await _client.PutAsJsonAsync($"/api/gym-management/gyms/{gym.Id}", new
        {
            name = "Updated Gym Name",
            description = "updated",
            address = "new street",
            platformTierId = (int?)null,
            isActive = true
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Update_ShouldReturnForbidden_WhenCallerHasNoMembership()
    {
        var (ownerId, _) = await SeedUserAsync("gym-update-owner-2");
        var gym = await SeedGymAsync(ownerId, "Update Deny Gym");
        var (_, outsiderToken) = await SeedUserAsync("gym-update-outsider");

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", outsiderToken);
        var response = await _client.PutAsJsonAsync($"/api/gym-management/gyms/{gym.Id}", new
        {
            name = "Should Not Apply",
            description = "n/a",
            address = "n/a",
            platformTierId = (int?)null,
            isActive = true
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ShouldReturnOk_WhenCallerIsGymOwner()
    {
        var (ownerId, ownerToken) = await SeedUserAsync("gym-delete-owner");
        var gym = await SeedGymAsync(ownerId, "Delete Owner Gym");

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
        var response = await _client.DeleteAsync($"/api/gym-management/gyms/{gym.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ShouldReturnForbidden_WhenCallerHasNoMembership()
    {
        var (ownerId, _) = await SeedUserAsync("gym-delete-owner-2");
        var gym = await SeedGymAsync(ownerId, "Delete Deny Gym");
        var (_, outsiderToken) = await SeedUserAsync("gym-delete-outsider");

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", outsiderToken);
        var response = await _client.DeleteAsync($"/api/gym-management/gyms/{gym.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<(int UserId, string Token)> SeedUserAsync(string suffix)
    {
        await using var context = fixture.CreateAuthorizationDbContext();
        var user = await TestDataSeeder.SeedUserAsync(context, $"{suffix}-{Guid.NewGuid():N}", CancellationToken.None);
        return (user.Id, TestFirebaseService.CreateToken(user.FirebaseUid, user.Email));
    }

    private async Task<Gym> SeedGymAsync(int ownerId, string name)
    {
        await using var context = fixture.CreateGymManagementDbContext();
        var gym = new Gym { OwnerId = ownerId, Name = $"{name}-{Guid.NewGuid():N}" };
        context.Gyms.Add(gym);
        await context.SaveChangesAsync();
        return gym;
    }
}
