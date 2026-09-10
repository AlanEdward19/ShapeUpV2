namespace IntegrationTests.Domains.Nutrition.Repositories;

using Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using ShapeUp.Features.Nutrition.Shared.Abstractions;
using ShapeUp.Features.Nutrition.Shared.Documents;

[Collection("SQL Server Write Operations")]
public sealed class MealPlanRepositoryIntegrationTests(SqlServerFixture fixture) : IAsyncLifetime
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

    [Fact]
    public async Task CreateAsync_GetByIdAsync_ShouldPersistAndRetrieve()
    {
        using var scope = _factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IMealPlanRepository>();

        var plan = new MealPlanDocument
        {
            Id = ObjectId.GenerateNewId().ToString(),
            UserId = 42,
            Name = $"Plan-{Guid.NewGuid():N}",
            IsActive = false,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            Items =
            [
                new MealPlanItemDocument
                {
                    MealSlot = "lunch",
                    FoodId = ObjectId.GenerateNewId().ToString(),
                    QuantityGramsOrMl = 150
                }
            ]
        };

        await repo.CreateAsync(plan, CancellationToken.None);

        var found = await repo.GetByIdAsync(plan.Id, CancellationToken.None);
        Assert.NotNull(found);
        Assert.Equal(plan.Name, found!.Name);
        Assert.Single(found.Items);
    }
}
