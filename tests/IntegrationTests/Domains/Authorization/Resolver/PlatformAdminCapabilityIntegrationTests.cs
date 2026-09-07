namespace IntegrationTests.Domains.Authorization.Resolver;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Infrastructure;

/// <summary>
/// Covers AD-003/AD-006 (native-authorization-model): capabilities prefixed "platform." require
/// PlatformRoleType.Admin, checked via the real HTTP pipeline (CapabilityPolicyProvider ->
/// CapabilityAuthorizationHandler -> CapabilityResolver -> IUserPlatformRoleRepository), exercised
/// here against ExercisesController.Create as the representative endpoint.
/// </summary>
[Collection("SQL Server Write Operations")]
public sealed class PlatformAdminCapabilityIntegrationTests(SqlServerFixture fixture) : IAsyncLifetime
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

    private async Task<(int UserId, string Token)> SeedUserAsync(bool asAdmin)
    {
        await using var context = fixture.CreateAuthorizationDbContext();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var user = await TestDataSeeder.SeedUserAsync(context, suffix, CancellationToken.None);

        if (asAdmin)
        {
            await using var gymContext = fixture.CreateGymManagementDbContext();
            await TestDataSeeder.GrantPlatformAdminAsync(gymContext, user.Id, CancellationToken.None);
        }

        return (user.Id, TestFirebaseService.CreateToken(user.FirebaseUid, user.Email));
    }

    private void Authorize(string token) =>
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    private static object ExercisePayload() => new
    {
        name = $"Exercise-{Guid.NewGuid():N}",
        namePt = $"Exercicio-{Guid.NewGuid():N}",
        videoUrl = (string?)null,
        muscles = Array.Empty<object>(),
        equipmentIds = Array.Empty<int>(),
        steps = Array.Empty<object>()
    };

    [Fact]
    public async Task Create_NonAdminUser_ReturnsForbidden()
    {
        var (_, token) = await SeedUserAsync(asAdmin: false);
        Authorize(token);

        var response = await _client.PostAsJsonAsync("/api/training/exercises", ExercisePayload());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Create_PlatformAdminUser_ReturnsCreated()
    {
        var (_, token) = await SeedUserAsync(asAdmin: true);
        Authorize(token);

        var response = await _client.PostAsJsonAsync("/api/training/exercises", ExercisePayload());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_NonAdminUser_StillAllowed()
    {
        var (_, token) = await SeedUserAsync(asAdmin: false);
        Authorize(token);

        var response = await _client.GetAsync("/api/training/exercises");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
