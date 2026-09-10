namespace IntegrationTests.Domains.Nutrition.Endpoints;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using ShapeUp.Features.Nutrition.Shared.Abstractions;

[Collection("SQL Server Write Operations")]
public sealed class MealPlanEndpointsIntegrationTests(SqlServerFixture fixture) : IAsyncLifetime
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
    public async Task CreateMealPlan_ShouldPersistPlanWithNullPrescribedByRelationshipId()
    {
        var auth = await SeedAuthorizedUserAsync();
        Authorize(auth.Token);

        var food = await CreateFoodAsync("Plan Food", 120, 8, 18, 3);

        var response = await _client.PostAsJsonAsync("/api/nutrition/meal-plans", new
        {
            name = "My Weekday Plan",
            items = new[]
            {
                new { mealSlot = "breakfast", foodId = food.Id, quantityGramsOrMl = 100m },
                new { mealSlot = "lunch", foodId = food.Id, quantityGramsOrMl = 150m }
            }
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var plan = await response.Content.ReadFromJsonAsync<MealPlanPayload>();
        Assert.NotNull(plan);
        Assert.Null(plan!.PrescribedByRelationshipId);
        Assert.Equal(2, plan.Items.Length);
    }

    [Fact]
    public async Task ActivateMealPlan_ShouldFillDiaryDayFromPlan()
    {
        var auth = await SeedAuthorizedUserAsync();
        Authorize(auth.Token);

        var food = await CreateFoodAsync("Activation Food", 200, 10, 30, 5);
        var create = await _client.PostAsJsonAsync("/api/nutrition/meal-plans", new
        {
            name = "Activation Plan",
            items = new[] { new { mealSlot = "dinner", foodId = food.Id, quantityGramsOrMl = 100m } }
        });
        var plan = await create.Content.ReadFromJsonAsync<MealPlanPayload>();
        Assert.NotNull(plan);

        var date = new DateOnly(2026, 6, 1);
        var activate = await _client.PostAsync($"/api/nutrition/meal-plans/{plan!.Id}/activate?date={date:yyyy-MM-dd}", null);
        Assert.Equal(HttpStatusCode.OK, activate.StatusCode);

        var payload = await activate.Content.ReadFromJsonAsync<ActivateMealPlanPayload>();
        Assert.NotNull(payload);
        Assert.True(payload!.Plan.IsActive);
        Assert.Equal(200, payload.DiaryDay.Totals.Kcal);
        Assert.Empty(payload.UnavailableItems);
    }

    [Fact]
    public async Task ActivateMealPlan_WhenFoodIsDeleted_ShouldSignalUnavailableWithoutApplyingItem()
    {
        var auth = await SeedAuthorizedUserAsync();
        Authorize(auth.Token);

        var deletedFood = await CreateFoodAsync("Deleted Food", 150, 12, 22, 6);

        var create = await _client.PostAsJsonAsync("/api/nutrition/meal-plans", new
        {
            name = "Deleted Food Plan",
            items = new[]
            {
                new { mealSlot = "breakfast", foodId = deletedFood.Id, quantityGramsOrMl = 100m },
                new { mealSlot = "lunch", foodId = deletedFood.Id, quantityGramsOrMl = 120m }
            }
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var plan = await create.Content.ReadFromJsonAsync<MealPlanPayload>();
        Assert.NotNull(plan);

        var admin = await SeedAuthorizedUserAsync(asAdmin: true);
        Authorize(admin.Token);

        var delete = await _client.DeleteAsync($"/api/nutrition/foods/{deletedFood.Id}");
        Assert.Equal(HttpStatusCode.OK, delete.StatusCode);

        Authorize(auth.Token);

        var date = new DateOnly(2026, 6, 2);
        var activate = await _client.PostAsync($"/api/nutrition/meal-plans/{plan!.Id}/activate?date={date:yyyy-MM-dd}", null);
        Assert.Equal(HttpStatusCode.OK, activate.StatusCode);

        var payload = await activate.Content.ReadFromJsonAsync<ActivateMealPlanPayload>();
        Assert.NotNull(payload);
        Assert.Equal(2, payload!.UnavailableItems.Length);
        Assert.All(payload.UnavailableItems, item => Assert.Equal(deletedFood.Id, item.FoodId));
        Assert.Equal(0, payload.DiaryDay.Totals.Kcal);
    }

    [Fact]
    public async Task DiaryEdit_ShouldNotMutateSavedMealPlan()
    {
        var auth = await SeedAuthorizedUserAsync();
        Authorize(auth.Token);

        var food = await CreateFoodAsync("Stable Plan Food", 90, 6, 15, 1);
        var create = await _client.PostAsJsonAsync("/api/nutrition/meal-plans", new
        {
            name = "Stable Plan",
            items = new[] { new { mealSlot = "lunch", foodId = food.Id, quantityGramsOrMl = 100m } }
        });
        var plan = await create.Content.ReadFromJsonAsync<MealPlanPayload>();
        Assert.NotNull(plan);

        var date = new DateOnly(2026, 6, 3);
        await _client.PostAsync($"/api/nutrition/meal-plans/{plan!.Id}/activate?date={date:yyyy-MM-dd}", null);

        await _client.PostAsJsonAsync("/api/nutrition/diary/entries", new
        {
            id = "manual-diary-edit",
            date,
            mealSlot = "snack",
            foodId = food.Id,
            quantityGramsOrMl = 200m
        });

        using var scope = _factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IMealPlanRepository>();
        var stored = await repository.GetByIdAsync(plan.Id, CancellationToken.None);

        Assert.NotNull(stored);
        Assert.Single(stored!.Items);
        Assert.Equal(100m, stored.Items[0].QuantityGramsOrMl);
        Assert.Equal("lunch", stored.Items[0].MealSlot);
    }

    private async Task<FoodPayload> CreateFoodAsync(string name, int kcal, int protein, int carb, int fat)
    {
        var response = await _client.PostAsJsonAsync("/api/nutrition/foods", new
        {
            name = $"{name}-{Guid.NewGuid():N}",
            barcode = $"789{Guid.NewGuid():N}"[..13],
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

    private async Task<AuthorizedUser> SeedAuthorizedUserAsync(bool asAdmin = false)
    {
        await using var context = fixture.CreateAuthorizationDbContext();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var user = await TestDataSeeder.SeedUserAsync(context, suffix, CancellationToken.None);

        if (asAdmin)
        {
            await using var gymContext = fixture.CreateGymManagementDbContext();
            await TestDataSeeder.GrantPlatformAdminAsync(gymContext, user.Id, CancellationToken.None);
        }

        return new AuthorizedUser(user.Id, TestFirebaseService.CreateToken(user.FirebaseUid, user.Email));
    }

    private sealed record AuthorizedUser(int UserId, string Token);
    private sealed record MacroPayload(int Kcal, int ProteinG, int CarbG, int FatG);
    private sealed record MealPlanItemPayload(string MealSlot, string FoodId, decimal QuantityGramsOrMl);
    private sealed record MealPlanPayload(
        string Id,
        string Name,
        int? PrescribedByRelationshipId,
        bool IsActive,
        MealPlanItemPayload[] Items);
    private sealed record DiaryDayPayload(DateOnly Date, object[] Meals, MacroPayload Totals);
    private sealed record UnavailableItemPayload(string MealSlot, string FoodId, decimal QuantityGramsOrMl, string Reason);
    private sealed record ActivateMealPlanPayload(MealPlanPayload Plan, DiaryDayPayload DiaryDay, UnavailableItemPayload[] UnavailableItems);
    private sealed record FoodPayload(string Id, string Name, MacroPayload MacrosPer100);
}
