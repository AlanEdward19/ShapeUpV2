namespace IntegrationTests.Domains.Nutrition.Endpoints;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using ShapeUp.Features.Nutrition.Fasting.Shared;
[Collection("SQL Server Write Operations")]
public sealed class FastingEndpointsIntegrationTests(SqlServerFixture fixture) : IAsyncLifetime
{
    private const string SaoPaulo = "America/Sao_Paulo";

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
    public async Task PutAgendaThenGetClock_ReturnsSavedAgendaAndAgendaSource()
    {
        var auth = await SeedAuthorizedUserAsync();
        Authorize(auth.Token);

        var put = await _client.PutAsJsonAsync("/api/nutrition/fasting/agenda", new
        {
            protocol = "16:8",
            eatingStartMinutes = 720,
            timeZone = SaoPaulo
        });
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);

        var get = await _client.GetAsync("/api/nutrition/fasting");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);

        var snapshot = await get.Content.ReadFromJsonAsync<FastingSnapshotPayload>();
        Assert.NotNull(snapshot);
        Assert.NotNull(snapshot!.Agenda);
        Assert.Equal("16:8", snapshot.Agenda!.Protocol);
        Assert.Equal(16, snapshot.Agenda.FastHours);
        Assert.Equal("Agenda", snapshot.Clock.Source);
    }

    [Fact]
    public async Task StartOverrideThenGetClock_ReportsOverrideSource()
    {
        var auth = await SeedAuthorizedUserAsync();
        Authorize(auth.Token);
        await SaveAgendaAsync();

        var start = await _client.PostAsync("/api/nutrition/fasting/override/start", null);
        Assert.Equal(HttpStatusCode.Created, start.StatusCode);

        var get = await _client.GetAsync("/api/nutrition/fasting");
        var snapshot = await get.Content.ReadFromJsonAsync<FastingSnapshotPayload>();
        Assert.NotNull(snapshot);
        Assert.NotNull(snapshot!.Override);
        Assert.Equal("Override", snapshot.Clock.Source);
        Assert.Equal("Fasting", snapshot.Override!.Status);
    }

    [Fact]
    public async Task CancelOverride_ReturnsClockToAgendaSource()
    {
        var auth = await SeedAuthorizedUserAsync();
        Authorize(auth.Token);
        await SaveAgendaAsync();

        var start = await _client.PostAsync("/api/nutrition/fasting/override/start", null);
        Assert.Equal(HttpStatusCode.Created, start.StatusCode);

        var cancel = await _client.PostAsync("/api/nutrition/fasting/override/cancel", null);
        Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);

        var get = await _client.GetAsync("/api/nutrition/fasting");
        var snapshot = await get.Content.ReadFromJsonAsync<FastingSnapshotPayload>();
        Assert.NotNull(snapshot);
        Assert.Null(snapshot!.Override);
        Assert.Equal("Agenda", snapshot.Clock.Source);
    }

    [Fact]
    public async Task StartOverride_WhenAlreadyActive_Returns409()
    {
        var auth = await SeedAuthorizedUserAsync();
        Authorize(auth.Token);
        await SaveAgendaAsync();

        var first = await _client.PostAsync("/api/nutrition/fasting/override/start", null);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await _client.PostAsync("/api/nutrition/fasting/override/start", null);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task GetClock_WhenUnauthenticated_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/nutrition/fasting");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetClock_WhenFeatureFlagDisabled_Returns404WithDisabledCode()
    {
        var auth = await SeedAuthorizedUserAsync();
        Authorize(auth.Token);

        await using var featureFlagsContext = fixture.CreatePlatformFeatureFlagsDbContext();
        var flag = await featureFlagsContext.Flags
            .SingleAsync(f => f.Key == FastingFeatureGuard.FeatureKey);
        var originalEnabled = flag.Enabled;

        try
        {
            flag.Enabled = false;
            await featureFlagsContext.SaveChangesAsync();

            var response = await _client.GetAsync("/api/nutrition/fasting");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

            var error = await response.Content.ReadFromJsonAsync<ErrorPayload>();
            Assert.NotNull(error);
            Assert.Equal("nutrition.fasting.disabled", error!.Code);
        }
        finally
        {
            flag.Enabled = originalEnabled;
            await featureFlagsContext.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task SetRecommendation_WithoutTrainingRelationship_Returns403()
    {
        var professional = await SeedAuthorizedUserAsync();
        var client = await SeedAuthorizedUserAsync();
        Authorize(professional.Token);

        var response = await _client.PutAsJsonAsync(
            $"/api/nutrition/fasting/recommendation/{client.UserId}",
            new { protocol = "18:6" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task SaveAgendaAsync()
    {
        var put = await _client.PutAsJsonAsync("/api/nutrition/fasting/agenda", new
        {
            protocol = "16:8",
            eatingStartMinutes = 720,
            timeZone = SaoPaulo
        });
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);
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

    private sealed record ErrorPayload(string Code, string Message);

    private sealed record FastingSnapshotPayload(
        FastingAgendaPayload? Agenda,
        FastingOverridePayload? Override,
        FastingRecommendationPayload? Recommendation,
        FastingClockPayload Clock);

    private sealed record FastingAgendaPayload(
        string Protocol,
        int FastHours,
        int EatHours,
        int EatingStartMinutes,
        string TimeZone,
        DateTime UpdatedAtUtc);

    private sealed record FastingOverridePayload(
        Guid Id,
        string Status,
        string Protocol,
        int FastHours,
        int EatHours,
        DateTime StartedAtUtc,
        DateTime FastEndsAtUtc,
        DateTime? EatEndsAtUtc);

    private sealed record FastingRecommendationPayload(string Protocol, int FastHours);

    private sealed record FastingClockPayload(string Status, DateTime? BoundaryAt, string? Source);
}
