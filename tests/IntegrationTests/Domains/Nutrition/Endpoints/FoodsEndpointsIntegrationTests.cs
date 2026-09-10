namespace IntegrationTests.Domains.Nutrition.Endpoints;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Infrastructure;

[Collection("SQL Server Write Operations")]
public sealed class FoodsEndpointsIntegrationTests(SqlServerFixture fixture) : IAsyncLifetime
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
    public async Task CreateFood_ShouldPersistPublicFoodWithRequiredMacros()
    {
        var auth = await SeedAuthorizedUserAsync();
        Authorize(auth.Token);

        var uniqueName = $"Oats-{Guid.NewGuid():N}";
        var response = await _client.PostAsJsonAsync("/api/nutrition/foods", new
        {
            name = uniqueName,
            macrosPer100 = new { kcal = 389, proteinG = 17, carbG = 66, fatG = 7 }
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<FoodPayload>();
        Assert.NotNull(created);
        Assert.Equal(uniqueName, created!.Name);
        Assert.Equal(389, created.MacrosPer100.Kcal);
        Assert.Null(created.MicrosPer100);
        Assert.Equal(auth.UserId, created.CreatedByUserId);

        Authorize((await SeedAuthorizedUserAsync()).Token);
        var search = await _client.GetAsync($"/api/nutrition/foods?query={Uri.EscapeDataString(uniqueName)}");
        Assert.Equal(HttpStatusCode.OK, search.StatusCode);

        var searchPayload = await search.Content.ReadFromJsonAsync<SearchFoodsPayload>();
        Assert.NotNull(searchPayload);
        Assert.Contains(searchPayload!.Items, x => x.Name == uniqueName);
    }

    [Fact]
    public async Task CreateFood_WhenRequiredFieldIsMissing_ReturnsValidationError()
    {
        var auth = await SeedAuthorizedUserAsync();
        Authorize(auth.Token);

        var response = await _client.PostAsJsonAsync("/api/nutrition/foods", new
        {
            name = string.Empty,
            macrosPer100 = new { kcal = 100, proteinG = 10, carbG = 20, fatG = 5 }
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ErrorPayload>();
        Assert.NotNull(error);
        Assert.Equal("validation_error", error!.Code);
        Assert.Contains("Name", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateFood_WhenBarcodeAlreadyExists_ReturnsConflict()
    {
        var auth = await SeedAuthorizedUserAsync();
        Authorize(auth.Token);

        var barcode = $"789{Guid.NewGuid():N}"[..13];

        var first = await _client.PostAsJsonAsync("/api/nutrition/foods", new
        {
            name = $"First-{Guid.NewGuid():N}",
            barcode,
            macrosPer100 = new { kcal = 100, proteinG = 10, carbG = 20, fatG = 5 }
        });
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var duplicate = await _client.PostAsJsonAsync("/api/nutrition/foods", new
        {
            name = $"Second-{Guid.NewGuid():N}",
            barcode,
            macrosPer100 = new { kcal = 120, proteinG = 12, carbG = 22, fatG = 6 }
        });

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        var error = await duplicate.Content.ReadFromJsonAsync<ErrorPayload>();
        Assert.NotNull(error);
        Assert.Equal("conflict", error!.Code);
        Assert.Contains("already exists", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SearchFoods_ShouldMatchNameCaseInsensitively()
    {
        var auth = await SeedAuthorizedUserAsync();
        Authorize(auth.Token);

        var token = Guid.NewGuid().ToString("N")[..8];
        var name = $"Quinoa Bowl {token}";

        var create = await _client.PostAsJsonAsync("/api/nutrition/foods", new
        {
            name,
            macrosPer100 = new { kcal = 120, proteinG = 4, carbG = 21, fatG = 2 }
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var search = await _client.GetAsync($"/api/nutrition/foods?query={Uri.EscapeDataString(token.ToUpperInvariant())}");
        Assert.Equal(HttpStatusCode.OK, search.StatusCode);

        var payload = await search.Content.ReadFromJsonAsync<SearchFoodsPayload>();
        Assert.NotNull(payload);
        Assert.Contains(payload!.Items, x => x.Name == name);
    }

    [Fact]
    public async Task SearchFoods_WhenQueryIsEmpty_ReturnsOkWithItemsCollection()
    {
        var auth = await SeedAuthorizedUserAsync();
        Authorize(auth.Token);

        var response = await _client.GetAsync("/api/nutrition/foods?query=");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<SearchFoodsPayload>();
        Assert.NotNull(payload);
        Assert.NotNull(payload!.Items);
    }

    [Fact]
    public async Task GetFoodByBarcode_WhenFoodExists_ReturnsFood()
    {
        var auth = await SeedAuthorizedUserAsync();
        Authorize(auth.Token);

        var barcode = $"789{Guid.NewGuid():N}"[..13];
        var create = await _client.PostAsJsonAsync("/api/nutrition/foods", new
        {
            name = $"Barcode Item {Guid.NewGuid():N}",
            barcode,
            macrosPer100 = new { kcal = 250, proteinG = 20, carbG = 30, fatG = 8 }
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var get = await _client.GetAsync($"/api/nutrition/foods/barcode/{Uri.EscapeDataString(barcode)}");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);

        var payload = await get.Content.ReadFromJsonAsync<FoodPayload>();
        Assert.NotNull(payload);
        Assert.Equal(barcode, payload!.Barcode);
    }

    [Fact]
    public async Task GetFoodByBarcode_WhenFoodDoesNotExist_ReturnsNotFound()
    {
        var auth = await SeedAuthorizedUserAsync();
        Authorize(auth.Token);

        var missingBarcode = $"789{Guid.NewGuid():N}"[..13];
        var response = await _client.GetAsync($"/api/nutrition/foods/barcode/{Uri.EscapeDataString(missingBarcode)}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ErrorPayload>();
        Assert.NotNull(error);
        Assert.Equal("not_found", error!.Code);
        Assert.Contains(missingBarcode, error.Message, StringComparison.Ordinal);
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
    private sealed record MacroPayload(int Kcal, int ProteinG, int CarbG, int FatG);
    private sealed record FoodPayload(
        string Id,
        string Name,
        string? Barcode,
        MacroPayload MacrosPer100,
        object? MicrosPer100,
        int CreatedByUserId,
        DateTime CreatedAtUtc);
    private sealed record SearchFoodsPayload(FoodPayload[] Items, string? NextCursor);
}
