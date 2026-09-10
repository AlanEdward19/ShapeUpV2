namespace IntegrationTests.Domains.Nutrition.Repositories;

using Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using ShapeUp.Features.Nutrition.Shared.Abstractions;
using ShapeUp.Features.Nutrition.Shared.Documents;
using ShapeUp.Features.Nutrition.Shared.ValueObjects;

[Collection("SQL Server Write Operations")]
public sealed class FoodRepositoryIntegrationTests(SqlServerFixture fixture) : IAsyncLifetime
{
    private IntegrationWebApplicationFactory _factory = null!;

    public Task InitializeAsync()
    {
        _factory = new IntegrationWebApplicationFactory(fixture);
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    private static FoodDocument NewFood(string name, string? barcode = null) => new()
    {
        Id = ObjectId.GenerateNewId().ToString(),
        Name = name,
        Barcode = barcode,
        MacrosPer100 = new MacroValueObject { Kcal = 100, ProteinG = 10, CarbG = 20, FatG = 5 },
        CreatedByUserId = 1,
        CreatedAtUtc = DateTime.UtcNow
    };

    [Fact]
    public async Task CreateAsync_GetByIdAsync_ShouldPersistAndRetrieve()
    {
        using var scope = _factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IFoodRepository>();
        var food = NewFood($"Chicken-{Guid.NewGuid():N}");

        await repo.CreateAsync(food, CancellationToken.None);

        var found = await repo.GetByIdAsync(food.Id, CancellationToken.None);
        Assert.NotNull(found);
        Assert.Equal(food.Name, found!.Name);
        Assert.Equal(food.MacrosPer100.ProteinG, found.MacrosPer100.ProteinG);
    }

    [Fact]
    public async Task SearchAsync_ShouldMatchNameCaseInsensitive()
    {
        using var scope = _factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IFoodRepository>();
        var uniqueToken = Guid.NewGuid().ToString("N")[..8];
        var food = NewFood($"Grilled Salmon {uniqueToken}");
        await repo.CreateAsync(food, CancellationToken.None);

        var (items, _) = await repo.SearchAsync(uniqueToken.ToUpperInvariant(), 10, null, CancellationToken.None);

        Assert.Contains(items, x => x.Id == food.Id);
    }

    [Fact]
    public async Task GetByBarcodeAsync_ShouldReturnMatchingFood()
    {
        using var scope = _factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IFoodRepository>();
        var barcode = $"789{Guid.NewGuid():N}"[..13];
        var food = NewFood($"Barcode Food {Guid.NewGuid():N}", barcode);
        await repo.CreateAsync(food, CancellationToken.None);

        var found = await repo.GetByBarcodeAsync(barcode, CancellationToken.None);

        Assert.NotNull(found);
        Assert.Equal(food.Id, found!.Id);
    }

    [Fact]
    public async Task SoftDeleteAsync_ShouldHideFromSearchAndGetById()
    {
        using var scope = _factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IFoodRepository>();
        var food = NewFood($"Delete Me {Guid.NewGuid():N}");
        await repo.CreateAsync(food, CancellationToken.None);

        var deleted = await repo.SoftDeleteAsync(food.Id, 99, DateTime.UtcNow, CancellationToken.None);
        Assert.True(deleted);

        var byId = await repo.GetByIdAsync(food.Id, CancellationToken.None);
        Assert.Null(byId);

        var (items, _) = await repo.SearchAsync(food.Name, 10, null, CancellationToken.None);
        Assert.DoesNotContain(items, x => x.Id == food.Id);

        var secondDelete = await repo.SoftDeleteAsync(food.Id, 99, DateTime.UtcNow, CancellationToken.None);
        Assert.False(secondDelete);
    }
}
