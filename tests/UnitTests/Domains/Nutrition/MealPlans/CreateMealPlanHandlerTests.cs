using ShapeUp.Features.Nutrition.MealPlans.CreateMealPlan;
using ShapeUp.Features.Nutrition.MealPlans.Shared.ViewModels;
using ShapeUp.Features.Nutrition.Shared.Abstractions;
using ShapeUp.Features.Nutrition.Shared.Documents;
using ShapeUp.Shared.Results;

namespace UnitTests.Domains.Nutrition.MealPlans;

public class CreateMealPlanHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldPersistPlanWithNullPrescribedByRelationshipId()
    {
        var foodId = "food-123";
        var repository = new FakeMealPlanRepository();
        var foodRepository = new FakeFoodRepository(foodId);
        var handler = new CreateMealPlanHandler(repository, foodRepository, new CreateMealPlanCommandValidator());

        var result = await handler.HandleAsync(
            new CreateMealPlanCommand(
                "Weekday Plan",
                [new MealPlanItemInputDto("lunch", foodId, 150m)]),
            7,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value!.PrescribedByRelationshipId);
        Assert.Single(repository.StoredPlans);
        Assert.Null(repository.StoredPlans[0].PrescribedByRelationshipId);
    }

    private sealed class FakeMealPlanRepository : IMealPlanRepository
    {
        public List<MealPlanDocument> StoredPlans { get; } = [];

        public Task CreateAsync(MealPlanDocument mealPlan, CancellationToken cancellationToken)
        {
            StoredPlans.Add(mealPlan);
            return Task.CompletedTask;
        }

        public Task<MealPlanDocument?> GetByIdAsync(string id, CancellationToken cancellationToken) =>
            Task.FromResult(StoredPlans.FirstOrDefault(p => p.Id == id));

        public Task<IReadOnlyList<MealPlanDocument>> GetByUserAsync(int userId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<MealPlanDocument>>(StoredPlans.Where(p => p.UserId == userId).ToList());

        public Task<MealPlanDocument?> GetActiveAsync(int userId, CancellationToken cancellationToken) =>
            Task.FromResult(StoredPlans.FirstOrDefault(p => p.UserId == userId && p.IsActive));

        public Task UpdateAsync(MealPlanDocument mealPlan, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeFoodRepository(string foodId) : IFoodRepository
    {
        public Task CreateAsync(FoodDocument food, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<FoodDocument?> GetByIdAsync(string id, CancellationToken cancellationToken) =>
            Task.FromResult(id == foodId ? new FoodDocument { Id = foodId, Name = "Rice", MacrosPer100 = new() { Kcal = 100, ProteinG = 2, CarbG = 20, FatG = 1 } } : null);

        public Task<FoodDocument?> GetByIdIncludingDeletedAsync(string id, CancellationToken cancellationToken) =>
            GetByIdAsync(id, cancellationToken);

        public Task<FoodDocument?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken) => Task.FromResult<FoodDocument?>(null);

        public Task<(IReadOnlyList<FoodDocument> Items, string? NextCursor)> SearchAsync(string query, int pageSize, string? cursor, CancellationToken cancellationToken) =>
            Task.FromResult<(IReadOnlyList<FoodDocument>, string?)>(([], null));

        public Task ApplyApprovedOverrideAsync(FoodOverrideDocument overrideDocument, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<bool> SoftDeleteAsync(string id, int deletedByUserId, DateTime deletedAtUtc, CancellationToken cancellationToken) =>
            Task.FromResult(false);
    }
}
