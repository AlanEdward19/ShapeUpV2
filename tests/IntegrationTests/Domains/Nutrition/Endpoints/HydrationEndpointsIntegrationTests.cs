namespace IntegrationTests.Domains.Nutrition.Endpoints;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Infrastructure;

[Collection("SQL Server Write Operations")]
public sealed class HydrationEndpointsIntegrationTests(SqlServerFixture fixture) : IAsyncLifetime
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
    public async Task PutThenGet_StoresAbsoluteTotalAndIsIdempotent()
    {
        await AuthorizeNewUserAsync();
        var date = DateTime.UtcNow.ToString("yyyy-MM-dd");

        var first = await _client.PutAsJsonAsync($"/api/nutrition/hydration/{date}", new { totalMl = 1750 });
        var retry = await _client.PutAsJsonAsync($"/api/nutrition/hydration/{date}", new { totalMl = 1750 });
        var read = await _client.GetFromJsonAsync<DayPayload>($"/api/nutrition/hydration?date={date}");
        var range = await _client.GetFromJsonAsync<RangePayload>($"/api/nutrition/hydration?from={date}&to={date}");

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
        Assert.Equal(1750, read!.TotalMl);
        Assert.Single(range!.Days);
    }

    [Fact]
    public async Task Put_WithTotalAboveLimit_ReturnsBadRequest()
    {
        await AuthorizeNewUserAsync();
        var date = DateTime.UtcNow.ToString("yyyy-MM-dd");

        var response = await _client.PutAsJsonAsync($"/api/nutrition/hydration/{date}", new { totalMl = 10001 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Put_WithoutTotal_ReturnsBadRequestAndKeepsTheDay()
    {
        await AuthorizeNewUserAsync();
        var date = DateTime.UtcNow.ToString("yyyy-MM-dd");
        await _client.PutAsJsonAsync($"/api/nutrition/hydration/{date}", new { totalMl = 1500 });

        var response = await _client.PutAsJsonAsync($"/api/nutrition/hydration/{date}", new { });
        var read = await _client.GetFromJsonAsync<DayPayload>($"/api/nutrition/hydration?date={date}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(1500, read!.TotalMl);
    }

    [Fact]
    public async Task Put_WithDifferentTotal_Overwrites()
    {
        await AuthorizeNewUserAsync();
        var date = DateTime.UtcNow.ToString("yyyy-MM-dd");

        await _client.PutAsJsonAsync($"/api/nutrition/hydration/{date}", new { totalMl = 500 });
        await _client.PutAsJsonAsync($"/api/nutrition/hydration/{date}", new { totalMl = 750 });
        var read = await _client.GetFromJsonAsync<DayPayload>($"/api/nutrition/hydration?date={date}");

        Assert.Equal(750, read!.TotalMl);
    }

    [Fact]
    public async Task Get_WithoutDateOrRange_ReturnsBadRequest()
    {
        await AuthorizeNewUserAsync();

        var response = await _client.GetAsync("/api/nutrition/hydration");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_ForDayWithoutRecord_ReturnsZero()
    {
        await AuthorizeNewUserAsync();

        var read = await _client.GetFromJsonAsync<DayPayload>("/api/nutrition/hydration?date=2026-01-01");

        Assert.Equal(0, read!.TotalMl);
    }

    private async Task AuthorizeNewUserAsync()
    {
        await using var context = fixture.CreateAuthorizationDbContext();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var user = await TestDataSeeder.SeedUserAsync(context, suffix, CancellationToken.None);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestFirebaseService.CreateToken(user.FirebaseUid, user.Email));
    }

    private sealed record DayPayload(DateOnly Date, int TotalMl, DateTime? UpdatedAtUtc);
    private sealed record RangePayload(DateOnly From, DateOnly To, DayPayload[] Days);
}
