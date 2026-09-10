namespace IntegrationTests.Domains.Nutrition.Endpoints;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Infrastructure;

[Collection("SQL Server Write Operations")]
public sealed class NutritionProfileEndpointsIntegrationTests(SqlServerFixture fixture) : IAsyncLifetime
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
    public async Task CompleteOnboarding_ShouldCalculateAndPersistMacroGoal()
    {
        var auth = await SeedAuthorizedUserAsync();
        Authorize(auth.Token);

        await RegisterWeightAsync(80m);

        var response = await _client.PostAsJsonAsync("/api/nutrition/profile/onboarding", new
        {
            heightCm = 180,
            age = 30,
            biologicalSex = "Male",
            activityLevel = "Moderate"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var profile = await response.Content.ReadFromJsonAsync<ProfilePayload>();
        Assert.NotNull(profile);
        Assert.NotNull(profile!.ActiveGoal);
        Assert.Equal(2759, profile.ActiveGoal!.Kcal);
        Assert.Equal(207, profile.ActiveGoal.ProteinG);
        Assert.False(profile.OnboardingSkipped);
    }

    [Fact]
    public async Task SetManualGoal_ShouldPersistExactGoalWithoutOnboarding()
    {
        var auth = await SeedAuthorizedUserAsync();
        Authorize(auth.Token);

        var response = await _client.PutAsJsonAsync("/api/nutrition/profile/goal", new
        {
            goal = new { kcal = 2200, proteinG = 150, carbG = 220, fatG = 70 }
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var profile = await response.Content.ReadFromJsonAsync<ProfilePayload>();
        Assert.NotNull(profile);
        Assert.True(profile!.OnboardingSkipped);
        Assert.Equal(2200, profile.ActiveGoal!.Kcal);
        Assert.Equal(150, profile.ActiveGoal.ProteinG);
        Assert.Equal(220, profile.ActiveGoal.CarbG);
        Assert.Equal(70, profile.ActiveGoal.FatG);
    }

    [Fact]
    public async Task CompleteOnboarding_WhenHeightIsImplausible_ReturnsValidationError()
    {
        var auth = await SeedAuthorizedUserAsync();
        Authorize(auth.Token);
        await RegisterWeightAsync(80m);

        var response = await _client.PostAsJsonAsync("/api/nutrition/profile/onboarding", new
        {
            heightCm = 50,
            age = 30,
            biologicalSex = "Male",
            activityLevel = "Moderate"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SetManualGoal_AfterOnboarding_ReplacesActiveGoalOnly()
    {
        var auth = await SeedAuthorizedUserAsync();
        Authorize(auth.Token);
        await RegisterWeightAsync(80m);

        var onboarding = await _client.PostAsJsonAsync("/api/nutrition/profile/onboarding", new
        {
            heightCm = 180,
            age = 30,
            biologicalSex = "Male",
            activityLevel = "Moderate"
        });
        Assert.Equal(HttpStatusCode.OK, onboarding.StatusCode);

        var manual = await _client.PutAsJsonAsync("/api/nutrition/profile/goal", new
        {
            goal = new { kcal = 2000, proteinG = 140, carbG = 200, fatG = 65 }
        });
        Assert.Equal(HttpStatusCode.OK, manual.StatusCode);

        var profile = await manual.Content.ReadFromJsonAsync<ProfilePayload>();
        Assert.NotNull(profile);
        Assert.Equal(2000, profile!.ActiveGoal!.Kcal);
        Assert.Equal(180, profile.HeightCm);
    }

    private async Task RegisterWeightAsync(decimal weight)
    {
        var register = await _client.PostAsJsonAsync("/api/nutrition/weight/registers", new
        {
            weight,
            dateUtc = DateTime.UtcNow
        });
        Assert.Equal(HttpStatusCode.OK, register.StatusCode);
    }

    private void Authorize(string token)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private async Task<AuthorizedUser> SeedAuthorizedUserAsync()
    {
        await using var context = fixture.CreateAuthorizationDbContext();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var user = await TestDataSeeder.SeedUserAsync(context, suffix, CancellationToken.None);

        return new AuthorizedUser(user.Id, TestFirebaseService.CreateToken(user.FirebaseUid, user.Email));
    }

    private sealed record AuthorizedUser(int UserId, string Token);
    private sealed record MacroGoalPayload(int Kcal, int ProteinG, int CarbG, int FatG);
    private sealed record ProfilePayload(
        int? HeightCm,
        int? Age,
        string? BiologicalSex,
        string? ActivityLevel,
        bool OnboardingSkipped,
        MacroGoalPayload? ActiveGoal,
        DateTime UpdatedAtUtc);
}
