using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using IntegrationTests.Infrastructure;
using ShapeUp.Features.GymManagement.Shared.Entities;

namespace IntegrationTests.Domains.GymManagement.Endpoints;

/// <summary>
/// T11: GymClientsController migrated from RequireScopesAttribute to [Authorize(Policy="capability:...")].
/// Covers AUTHZ-01/AUTHZ-02 (membership-based allow) and AUTHZ-04 (deny without membership, including cross-gym).
/// </summary>
[Collection("SQL Server Write Operations")]
public sealed class GymClientsEndpointsIntegrationTests(SqlServerFixture fixture) : IAsyncLifetime
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
    public async Task GetAll_ShouldReturnOk_WhenCallerIsActiveGymStaff()
    {
        var gym = await SeedGymAsync();
        var staff = await SeedTokenedUserAsync("get-all-staff");
        await SeedGymStaffAsync(gym.Id, staff.UserId, GymStaffRole.Staff);
        AuthorizeAs(staff.Token);

        var response = await _client.GetAsync($"/api/gym-management/gyms/{gym.Id}/clients");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_ShouldReturnForbidden_WhenCallerHasNoMembership()
    {
        var gym = await SeedGymAsync();
        var outsider = await SeedTokenedUserAsync("get-all-outsider");
        AuthorizeAs(outsider.Token);

        var response = await _client.GetAsync($"/api/gym-management/gyms/{gym.Id}/clients");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_ShouldReturnForbidden_WhenCallerIsStaffOfDifferentGym()
    {
        var gymA = await SeedGymAsync();
        var gymB = await SeedGymAsync();
        var staffOfB = await SeedTokenedUserAsync("get-all-cross-gym");
        await SeedGymStaffAsync(gymB.Id, staffOfB.UserId, GymStaffRole.Staff);
        AuthorizeAs(staffOfB.Token);

        var response = await _client.GetAsync($"/api/gym-management/gyms/{gymA.Id}/clients");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Enroll_ShouldReturnCreated_WhenCallerIsActiveGymStaff()
    {
        // Role is plain "Staff" (not Owner/Receptionist) on purpose: proves the ad-hoc
        // IsOwnerOrReceptionistAsync check was removed in favor of the capability policy.
        var gym = await SeedGymAsync();
        var plan = await SeedGymPlanAsync(gym.Id);
        var staff = await SeedTokenedUserAsync("enroll-staff");
        await SeedGymStaffAsync(gym.Id, staff.UserId, GymStaffRole.Staff);
        AuthorizeAs(staff.Token);

        var response = await _client.PostAsJsonAsync($"/api/gym-management/gyms/{gym.Id}/clients", new
        {
            gymId = gym.Id,
            userId = Random.Shared.Next(100_000, 200_000),
            gymPlanId = plan.Id,
            trainerId = (int?)null
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Enroll_ShouldReturnForbidden_WhenCallerHasNoMembership()
    {
        var gym = await SeedGymAsync();
        var plan = await SeedGymPlanAsync(gym.Id);
        var outsider = await SeedTokenedUserAsync("enroll-outsider");
        AuthorizeAs(outsider.Token);

        var response = await _client.PostAsJsonAsync($"/api/gym-management/gyms/{gym.Id}/clients", new
        {
            gymId = gym.Id,
            userId = Random.Shared.Next(200_000, 300_000),
            gymPlanId = plan.Id,
            trainerId = (int?)null
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Enroll_ShouldReturnForbidden_WhenCallerIsStaffOfDifferentGym()
    {
        var gymA = await SeedGymAsync();
        var gymB = await SeedGymAsync();
        var plan = await SeedGymPlanAsync(gymA.Id);
        var staffOfB = await SeedTokenedUserAsync("enroll-cross-gym");
        await SeedGymStaffAsync(gymB.Id, staffOfB.UserId, GymStaffRole.Staff);
        AuthorizeAs(staffOfB.Token);

        var response = await _client.PostAsJsonAsync($"/api/gym-management/gyms/{gymA.Id}/clients", new
        {
            gymId = gymA.Id,
            userId = Random.Shared.Next(300_000, 400_000),
            gymPlanId = plan.Id,
            trainerId = (int?)null
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AssignTrainer_ShouldReturnOk_WhenCallerIsActiveGymStaff()
    {
        var gym = await SeedGymAsync();
        var plan = await SeedGymPlanAsync(gym.Id);
        var client = await SeedGymClientAsync(gym.Id, plan.Id);
        var trainer = await SeedGymStaffMemberAsync(gym.Id, GymStaffRole.Trainer);
        var staff = await SeedTokenedUserAsync("assign-staff");
        await SeedGymStaffAsync(gym.Id, staff.UserId, GymStaffRole.Staff);
        AuthorizeAs(staff.Token);

        var response = await _client.PutAsJsonAsync(
            $"/api/gym-management/gyms/{gym.Id}/clients/{client.Id}/trainer",
            new { gymId = gym.Id, clientId = client.Id, trainerId = trainer.Id });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AssignTrainer_ShouldReturnForbidden_WhenCallerHasNoMembership()
    {
        var gym = await SeedGymAsync();
        var plan = await SeedGymPlanAsync(gym.Id);
        var client = await SeedGymClientAsync(gym.Id, plan.Id);
        var trainer = await SeedGymStaffMemberAsync(gym.Id, GymStaffRole.Trainer);
        var outsider = await SeedTokenedUserAsync("assign-outsider");
        AuthorizeAs(outsider.Token);

        var response = await _client.PutAsJsonAsync(
            $"/api/gym-management/gyms/{gym.Id}/clients/{client.Id}/trainer",
            new { gymId = gym.Id, clientId = client.Id, trainerId = trainer.Id });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AssignTrainer_ShouldReturnForbidden_WhenCallerIsStaffOfDifferentGym()
    {
        var gymA = await SeedGymAsync();
        var gymB = await SeedGymAsync();
        var plan = await SeedGymPlanAsync(gymA.Id);
        var client = await SeedGymClientAsync(gymA.Id, plan.Id);
        var trainer = await SeedGymStaffMemberAsync(gymA.Id, GymStaffRole.Trainer);
        var staffOfB = await SeedTokenedUserAsync("assign-cross-gym");
        await SeedGymStaffAsync(gymB.Id, staffOfB.UserId, GymStaffRole.Staff);
        AuthorizeAs(staffOfB.Token);

        var response = await _client.PutAsJsonAsync(
            $"/api/gym-management/gyms/{gymA.Id}/clients/{client.Id}/trainer",
            new { gymId = gymA.Id, clientId = client.Id, trainerId = trainer.Id });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private void AuthorizeAs(string token) =>
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    private async Task<Gym> SeedGymAsync()
    {
        await using var context = fixture.CreateGymManagementDbContext();
        var gym = new Gym
        {
            OwnerId = Random.Shared.Next(1_000_000, 2_000_000),
            Name = $"Gym-{Guid.NewGuid():N}"
        };
        context.Gyms.Add(gym);
        await context.SaveChangesAsync();
        return gym;
    }

    private async Task<GymPlan> SeedGymPlanAsync(int gymId)
    {
        await using var context = fixture.CreateGymManagementDbContext();
        var plan = new GymPlan
        {
            GymId = gymId,
            Name = $"Plan-{Guid.NewGuid():N}",
            Price = 99.90m,
            DurationDays = 30
        };
        context.GymPlans.Add(plan);
        await context.SaveChangesAsync();
        return plan;
    }

    private async Task<GymClient> SeedGymClientAsync(int gymId, int planId)
    {
        await using var context = fixture.CreateGymManagementDbContext();
        var client = new GymClient
        {
            GymId = gymId,
            UserId = Random.Shared.Next(400_000, 500_000),
            GymPlanId = planId
        };
        context.GymClients.Add(client);
        await context.SaveChangesAsync();
        return client;
    }

    private async Task<GymStaff> SeedGymStaffMemberAsync(int gymId, GymStaffRole role)
    {
        await using var context = fixture.CreateGymManagementDbContext();
        var staff = new GymStaff
        {
            GymId = gymId,
            UserId = Random.Shared.Next(500_000, 600_000),
            Role = role,
            IsActive = true
        };
        context.GymStaff.Add(staff);
        await context.SaveChangesAsync();
        return staff;
    }

    private async Task SeedGymStaffAsync(int gymId, int userId, GymStaffRole role)
    {
        await using var context = fixture.CreateGymManagementDbContext();
        context.GymStaff.Add(new GymStaff
        {
            GymId = gymId,
            UserId = userId,
            Role = role,
            IsActive = true
        });
        await context.SaveChangesAsync();
    }

    private async Task<(int UserId, string Token)> SeedTokenedUserAsync(string suffix)
    {
        await using var context = fixture.CreateAuthorizationDbContext();
        var uniqueSuffix = $"{suffix}-{Guid.NewGuid():N}";
        var user = await TestDataSeeder.SeedUserAsync(context, uniqueSuffix, CancellationToken.None);
        var token = TestFirebaseService.CreateToken(user.FirebaseUid, user.Email);
        return (user.Id, token);
    }
}
