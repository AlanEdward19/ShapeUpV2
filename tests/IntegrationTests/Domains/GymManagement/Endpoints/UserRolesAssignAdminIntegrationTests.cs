using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using ShapeUp.Features.GymManagement.Shared.Entities;

namespace IntegrationTests.Domains.GymManagement.Endpoints;

/// <summary>Only a platform admin can hand out the Admin role.</summary>
[Collection("SQL Server Write Operations")]
public sealed class UserRolesAssignAdminIntegrationTests(SqlServerFixture fixture) : IAsyncLifetime
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
    public async Task AssignAdmin_ByNonAdmin_ShouldReturnForbiddenAndNotGrantTheRole()
    {
        var caller = await SeedUserAsync(asAdmin: false);
        var target = await SeedUserAsync(asAdmin: false);
        Authorize(caller.Token);

        var response = await _client.PostAsJsonAsync("/api/gym-management/user-roles", new { userId = target.UserId, role = "Admin" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await using var gym = fixture.CreateGymManagementDbContext();
        Assert.False(await gym.UserPlatformRoles.AnyAsync(r => r.UserId == target.UserId && r.Role == PlatformRoleType.Admin));
    }

    [Fact]
    public async Task AssignAdmin_ByAdmin_ShouldCreateTheAdminRole()
    {
        var admin = await SeedUserAsync(asAdmin: true);
        var target = await SeedUserAsync(asAdmin: false);
        Authorize(admin.Token);

        var response = await _client.PostAsJsonAsync("/api/gym-management/user-roles", new { userId = target.UserId, role = "Admin" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        await using var gym = fixture.CreateGymManagementDbContext();
        Assert.True(await gym.UserPlatformRoles.AnyAsync(r => r.UserId == target.UserId && r.Role == PlatformRoleType.Admin && r.IsActive));
    }

    private void Authorize(string token) =>
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    private async Task<TestUser> SeedUserAsync(bool asAdmin)
    {
        await using var context = fixture.CreateAuthorizationDbContext();
        var user = await TestDataSeeder.SeedUserAsync(context, Guid.NewGuid().ToString("N")[..8], CancellationToken.None);
        if (asAdmin)
        {
            await using var gym = fixture.CreateGymManagementDbContext();
            await TestDataSeeder.GrantPlatformAdminAsync(gym, user.Id, CancellationToken.None);
        }

        return new TestUser(user.Id, TestFirebaseService.CreateToken(user.FirebaseUid, user.Email));
    }

    private sealed record TestUser(int UserId, string Token);
}
