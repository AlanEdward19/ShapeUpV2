namespace IntegrationTests.Domains.Nutrition.Endpoints;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Infrastructure;

[Collection("SQL Server Write Operations")]
public sealed class DiaryEndpointsIntegrationTests(SqlServerFixture fixture) : IAsyncLifetime
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
    public async Task GetDiaryDay_WhenNoEntriesExist_ReturnsEmptyDayWithZeroTotals()
    {
        var auth = await SeedAuthorizedUserAsync();
        Authorize(auth.Token);

        var response = await _client.GetAsync("/api/nutrition/diary?date=2026-05-01");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var day = await response.Content.ReadFromJsonAsync<DiaryDayPayload>();
        Assert.NotNull(day);
        Assert.Equal(new DateOnly(2026, 5, 1), day!.Date);
        Assert.Empty(day.Meals);
        Assert.Equal(0, day.Totals.Kcal);
    }

    [Fact]
    public async Task AddDiaryEntry_ShouldGroupByMealAndCalculateTotals()
    {
        var auth = await SeedAuthorizedUserAsync();
        Authorize(auth.Token);

        var food = await CreateFoodAsync("Diary Rice", 200, 8, 40, 2);
        var date = new DateOnly(2026, 5, 2);

        var breakfast = await _client.PostAsJsonAsync("/api/nutrition/diary/entries", new
        {
            id = "entry-breakfast-1",
            date,
            mealSlot = "breakfast",
            foodId = food.Id,
            quantityGramsOrMl = 100m
        });
        Assert.Equal(HttpStatusCode.OK, breakfast.StatusCode);

        var lunch = await _client.PostAsJsonAsync("/api/nutrition/diary/entries", new
        {
            id = "entry-lunch-1",
            date,
            mealSlot = "lunch",
            foodId = food.Id,
            quantityGramsOrMl = 50m
        });
        Assert.Equal(HttpStatusCode.OK, lunch.StatusCode);

        var day = await lunch.Content.ReadFromJsonAsync<DiaryDayPayload>();
        Assert.NotNull(day);
        Assert.Equal(2, day!.Meals.Length);
        Assert.Equal(300, day.Totals.Kcal);
        Assert.Equal(12, day.Totals.ProteinG);
    }

    [Fact]
    public async Task AddDiaryEntry_WhenSameIdIsRetried_IsIdempotent()
    {
        var auth = await SeedAuthorizedUserAsync();
        Authorize(auth.Token);

        var food = await CreateFoodAsync("Idempotent Beans", 90, 6, 15, 1);
        var date = new DateOnly(2026, 5, 3);
        var payload = new
        {
            id = "entry-idempotent-1",
            date,
            mealSlot = "dinner",
            foodId = food.Id,
            quantityGramsOrMl = 100m
        };

        var first = await _client.PostAsJsonAsync("/api/nutrition/diary/entries", payload);
        var second = await _client.PostAsJsonAsync("/api/nutrition/diary/entries", payload);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        var get = await _client.GetAsync("/api/nutrition/diary?date=2026-05-03");
        var day = await get.Content.ReadFromJsonAsync<DiaryDayPayload>();
        Assert.NotNull(day);
        Assert.Single(day!.Meals);
        Assert.Single(day.Meals[0].Items);
        Assert.Equal(90, day.Totals.Kcal);
    }

    [Fact]
    public async Task AddDiaryEntry_WhenOverrideIsActive_UsesOverrideMacros()
    {
        var auth = await SeedAuthorizedUserAsync();
        Authorize(auth.Token);

        var food = await CreateFoodAsync("Override Pasta", 100, 10, 20, 5);
        await _client.PostAsJsonAsync(
            $"/api/nutrition/foods/{food.Id}/override",
            new { macrosPer100 = new { kcal = 140, proteinG = 12, carbG = 22, fatG = 6 } });

        var response = await _client.PostAsJsonAsync("/api/nutrition/diary/entries", new
        {
            id = "entry-override-1",
            date = new DateOnly(2026, 5, 4),
            mealSlot = "lunch",
            foodId = food.Id,
            quantityGramsOrMl = 100m
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var day = await response.Content.ReadFromJsonAsync<DiaryDayPayload>();
        Assert.NotNull(day);
        Assert.Equal(140, day!.Totals.Kcal);
        Assert.True(day.Meals[0].Items[0].UsesOverride);
    }

    [Fact]
    public async Task RemoveDiaryEntry_ShouldRecalculateDayTotals()
    {
        var auth = await SeedAuthorizedUserAsync();
        Authorize(auth.Token);

        var food = await CreateFoodAsync("Removable Oats", 389, 17, 66, 7);
        var date = new DateOnly(2026, 5, 5);

        await _client.PostAsJsonAsync("/api/nutrition/diary/entries", new
        {
            id = "entry-remove-1",
            date,
            mealSlot = "breakfast",
            foodId = food.Id,
            quantityGramsOrMl = 100m
        });

        await _client.PostAsJsonAsync("/api/nutrition/diary/entries", new
        {
            id = "entry-remove-2",
            date,
            mealSlot = "lunch",
            foodId = food.Id,
            quantityGramsOrMl = 50m
        });

        var remove = await _client.DeleteAsync($"/api/nutrition/diary/entries/entry-remove-1?date={date:yyyy-MM-dd}");
        Assert.Equal(HttpStatusCode.OK, remove.StatusCode);

        var day = await remove.Content.ReadFromJsonAsync<DiaryDayPayload>();
        Assert.NotNull(day);
        Assert.Equal(195, day!.Totals.Kcal);
        Assert.Single(day.Meals[0].Items);
    }

    private async Task<FoodPayload> CreateFoodAsync(string name, int kcal, int protein, int carb, int fat)
    {
        var response = await _client.PostAsJsonAsync("/api/nutrition/foods", new
        {
            name = $"{name}-{Guid.NewGuid():N}",
            macrosPer100 = new { kcal, proteinG = protein, carbG = carb, fatG = fat }
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var food = await response.Content.ReadFromJsonAsync<FoodPayload>();
        Assert.NotNull(food);
        return food!;
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
    private sealed record MacroPayload(int Kcal, int ProteinG, int CarbG, int FatG);
    private sealed record DiaryEntryPayload(
        string Id,
        string MealSlot,
        string FoodId,
        bool UsesOverride,
        decimal QuantityGramsOrMl,
        MacroPayload ComputedMacros);
    private sealed record DiaryMealPayload(string MealSlot, DiaryEntryPayload[] Items);
    private sealed record DiaryDayPayload(DateOnly Date, DiaryMealPayload[] Meals, MacroPayload Totals);
    private sealed record FoodPayload(string Id, string Name, MacroPayload MacrosPer100);
}
