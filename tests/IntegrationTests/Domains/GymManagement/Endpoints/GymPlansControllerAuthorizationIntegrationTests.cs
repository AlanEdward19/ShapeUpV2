namespace IntegrationTests.Domains.GymManagement.Endpoints;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Infrastructure;
using ShapeUp.Features.GymManagement.Infrastructure.Repositories;
using ShapeUp.Features.GymManagement.Shared.Entities;

/// <summary>
/// Covers T10 (native-authorization-model): GymPlansController now authorizes every endpoint via
/// [Authorize(Policy="capability:gym.plans.*")] instead of RequireScopesAttribute. Exercises the
/// real HTTP pipeline (CapabilityPolicyProvider -> CapabilityAuthorizationHandler -> CapabilityResolver
/// -> OrganizationMembershipAdapter) against SQL Server, per spec.md AUTHZ-01/AUTHZ-02/AUTHZ-04.
/// </summary>
[Collection("SQL Server Write Operations")]
public sealed class GymPlansControllerAuthorizationIntegrationTests(SqlServerFixture fixture) : IAsyncLifetime
{
    private IntegrationWebApplicationFactory _factory = null!;

    public Task InitializeAsync()
    {
        _factory = new IntegrationWebApplicationFactory(fixture);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    private async Task<(int UserId, string FirebaseUid, string Email)> SeedUserAsync(string suffix)
    {
        await using var ctx = fixture.CreateAuthorizationDbContext();
        var user = await TestDataSeeder.SeedUserAsync(ctx, $"{suffix}-{Guid.NewGuid():N}", CancellationToken.None);
        return (user.Id, user.FirebaseUid, user.Email!);
    }

    private async Task<int> SeedGymAsync(int ownerId)
    {
        await using var ctx = fixture.CreateGymManagementDbContext();
        var repo = new GymRepository(ctx);
        var gym = new Gym { OwnerId = ownerId, Name = $"Gym-{Guid.NewGuid():N}" };
        await repo.AddAsync(gym, CancellationToken.None);
        return gym.Id;
    }

    private async Task AddStaffAsync(int gymId, int userId, GymStaffRole role)
    {
        await using var ctx = fixture.CreateGymManagementDbContext();
        var repo = new GymStaffRepository(ctx);
        await repo.AddAsync(new GymStaff { GymId = gymId, UserId = userId, Role = role }, CancellationToken.None);
    }

    private HttpClient CreateAuthorizedClient(string firebaseUid, string email)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestFirebaseService.CreateToken(firebaseUid, email));
        return client;
    }

    private static object PlanPayload(int gymId, string name) => new
    {
        gymId,
        name,
        description = "integration",
        price = 49.90,
        durationDays = 30
    };

    // ---------- GetAll (capability:gym.plans.read) ----------

    [Fact]
    public async Task GetAll_OwnerWithMembership_ReturnsOk()
    {
        var owner = await SeedUserAsync("owner");
        var gymId = await SeedGymAsync(owner.UserId);
        using var client = CreateAuthorizedClient(owner.FirebaseUid, owner.Email);

        var response = await client.GetAsync($"/api/gym-management/gyms/{gymId}/plans");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_UserWithoutMembership_ReturnsForbidden()
    {
        var owner = await SeedUserAsync("owner");
        var outsider = await SeedUserAsync("outsider");
        var gymId = await SeedGymAsync(owner.UserId);
        using var client = CreateAuthorizedClient(outsider.FirebaseUid, outsider.Email);

        var response = await client.GetAsync($"/api/gym-management/gyms/{gymId}/plans");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_StaffOfAnotherGym_ReturnsForbidden()
    {
        var ownerA = await SeedUserAsync("owner-a");
        var ownerB = await SeedUserAsync("owner-b");
        var staffOfB = await SeedUserAsync("staff-b");
        var gymAId = await SeedGymAsync(ownerA.UserId);
        var gymBId = await SeedGymAsync(ownerB.UserId);
        await AddStaffAsync(gymBId, staffOfB.UserId, GymStaffRole.Receptionist);
        using var client = CreateAuthorizedClient(staffOfB.FirebaseUid, staffOfB.Email);

        var response = await client.GetAsync($"/api/gym-management/gyms/{gymAId}/plans");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---------- Create (capability:gym.plans.create) ----------

    [Fact]
    public async Task Create_OwnerWithMembership_ReturnsCreated()
    {
        var owner = await SeedUserAsync("owner");
        var gymId = await SeedGymAsync(owner.UserId);
        using var client = CreateAuthorizedClient(owner.FirebaseUid, owner.Email);

        var response = await client.PostAsJsonAsync($"/api/gym-management/gyms/{gymId}/plans", PlanPayload(gymId, "Monthly"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Create_UserWithoutMembership_ReturnsForbidden()
    {
        var owner = await SeedUserAsync("owner");
        var outsider = await SeedUserAsync("outsider");
        var gymId = await SeedGymAsync(owner.UserId);
        using var client = CreateAuthorizedClient(outsider.FirebaseUid, outsider.Email);

        var response = await client.PostAsJsonAsync($"/api/gym-management/gyms/{gymId}/plans", PlanPayload(gymId, "Monthly"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Create_StaffOfAnotherGym_ReturnsForbidden()
    {
        var ownerA = await SeedUserAsync("owner-a");
        var ownerB = await SeedUserAsync("owner-b");
        var staffOfB = await SeedUserAsync("staff-b");
        var gymAId = await SeedGymAsync(ownerA.UserId);
        var gymBId = await SeedGymAsync(ownerB.UserId);
        await AddStaffAsync(gymBId, staffOfB.UserId, GymStaffRole.Trainer);
        using var client = CreateAuthorizedClient(staffOfB.FirebaseUid, staffOfB.Email);

        var response = await client.PostAsJsonAsync($"/api/gym-management/gyms/{gymAId}/plans", PlanPayload(gymAId, "Monthly"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---------- Update (capability:gym.plans.update) ----------

    [Fact]
    public async Task Update_OwnerWithMembership_ReturnsOk()
    {
        var owner = await SeedUserAsync("owner");
        var gymId = await SeedGymAsync(owner.UserId);
        using var client = CreateAuthorizedClient(owner.FirebaseUid, owner.Email);
        var planId = await CreatePlanAsync(client, gymId);

        var response = await client.PutAsJsonAsync($"/api/gym-management/gyms/{gymId}/plans/{planId}", new
        {
            planId,
            gymId,
            name = "Updated",
            description = "integration",
            price = 59.90,
            durationDays = 60,
            isActive = true
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Update_UserWithoutMembership_ReturnsForbidden()
    {
        var owner = await SeedUserAsync("owner");
        var outsider = await SeedUserAsync("outsider");
        var gymId = await SeedGymAsync(owner.UserId);
        using var ownerClient = CreateAuthorizedClient(owner.FirebaseUid, owner.Email);
        var planId = await CreatePlanAsync(ownerClient, gymId);
        using var client = CreateAuthorizedClient(outsider.FirebaseUid, outsider.Email);

        var response = await client.PutAsJsonAsync($"/api/gym-management/gyms/{gymId}/plans/{planId}", new
        {
            planId,
            gymId,
            name = "Updated",
            description = "integration",
            price = 59.90,
            durationDays = 60,
            isActive = true
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Update_StaffOfAnotherGym_ReturnsForbidden()
    {
        var ownerA = await SeedUserAsync("owner-a");
        var ownerB = await SeedUserAsync("owner-b");
        var staffOfB = await SeedUserAsync("staff-b");
        var gymAId = await SeedGymAsync(ownerA.UserId);
        var gymBId = await SeedGymAsync(ownerB.UserId);
        await AddStaffAsync(gymBId, staffOfB.UserId, GymStaffRole.Manager);
        using var ownerClient = CreateAuthorizedClient(ownerA.FirebaseUid, ownerA.Email);
        var planId = await CreatePlanAsync(ownerClient, gymAId);
        using var client = CreateAuthorizedClient(staffOfB.FirebaseUid, staffOfB.Email);

        var response = await client.PutAsJsonAsync($"/api/gym-management/gyms/{gymAId}/plans/{planId}", new
        {
            planId,
            gymId = gymAId,
            name = "Updated",
            description = "integration",
            price = 59.90,
            durationDays = 60,
            isActive = true
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---------- Delete (capability:gym.plans.delete) ----------

    [Fact]
    public async Task Delete_OwnerWithMembership_ReturnsOk()
    {
        var owner = await SeedUserAsync("owner");
        var gymId = await SeedGymAsync(owner.UserId);
        using var client = CreateAuthorizedClient(owner.FirebaseUid, owner.Email);
        var planId = await CreatePlanAsync(client, gymId);

        var response = await client.DeleteAsync($"/api/gym-management/gyms/{gymId}/plans/{planId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Delete_UserWithoutMembership_ReturnsForbidden()
    {
        var owner = await SeedUserAsync("owner");
        var outsider = await SeedUserAsync("outsider");
        var gymId = await SeedGymAsync(owner.UserId);
        using var ownerClient = CreateAuthorizedClient(owner.FirebaseUid, owner.Email);
        var planId = await CreatePlanAsync(ownerClient, gymId);
        using var client = CreateAuthorizedClient(outsider.FirebaseUid, outsider.Email);

        var response = await client.DeleteAsync($"/api/gym-management/gyms/{gymId}/plans/{planId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Delete_StaffOfAnotherGym_ReturnsForbidden()
    {
        var ownerA = await SeedUserAsync("owner-a");
        var ownerB = await SeedUserAsync("owner-b");
        var staffOfB = await SeedUserAsync("staff-b");
        var gymAId = await SeedGymAsync(ownerA.UserId);
        var gymBId = await SeedGymAsync(ownerB.UserId);
        await AddStaffAsync(gymBId, staffOfB.UserId, GymStaffRole.Finance);
        using var ownerClient = CreateAuthorizedClient(ownerA.FirebaseUid, ownerA.Email);
        var planId = await CreatePlanAsync(ownerClient, gymAId);
        using var client = CreateAuthorizedClient(staffOfB.FirebaseUid, staffOfB.Email);

        var response = await client.DeleteAsync($"/api/gym-management/gyms/{gymAId}/plans/{planId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static async Task<int> CreatePlanAsync(HttpClient client, int gymId)
    {
        var response = await client.PostAsJsonAsync($"/api/gym-management/gyms/{gymId}/plans", PlanPayload(gymId, $"Plan-{Guid.NewGuid():N}"));
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<CreatedPlanPayload>();
        return payload!.Id;
    }

    private sealed record CreatedPlanPayload(int Id, int GymId, string Name);
}
