using Moq;
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
        var handler = new CreateMealPlanHandler(repository, foodRepository, AllowAll(), new CreateMealPlanCommandValidator());

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

    [Fact]
    public async Task HandleAsync_WithTargetUser_PersistsPlanForTargetPrescribedByActor()
    {
        var repository = new FakeMealPlanRepository();
        var access = new Mock<INutritionAccessPolicy>();
        access.Setup(a => a.CanManageNutritionForAsync(7, 9, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var handler = new CreateMealPlanHandler(repository, new FakeFoodRepository("f1"), access.Object, new CreateMealPlanCommandValidator());

        var result = await handler.HandleAsync(
            new CreateMealPlanCommand("Cut", [new MealPlanItemInputDto("lunch", "f1", 100m)], TargetUserId: 9),
            7,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(9, repository.StoredPlans[0].UserId);
        Assert.Equal(7, repository.StoredPlans[0].PrescribedByUserId);
        Assert.Equal(7, result.Value!.PrescribedByUserId);
    }

    [Fact]
    public async Task HandleAsync_WithTargetUserWithoutAccess_ReturnsForbiddenAndPersistsNothing()
    {
        var repository = new FakeMealPlanRepository();
        var access = new Mock<INutritionAccessPolicy>();
        access.Setup(a => a.CanManageNutritionForAsync(7, 9, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var handler = new CreateMealPlanHandler(repository, new FakeFoodRepository("f1"), access.Object, new CreateMealPlanCommandValidator());

        var result = await handler.HandleAsync(
            new CreateMealPlanCommand("Cut", [new MealPlanItemInputDto("lunch", "f1", 100m)], TargetUserId: 9),
            7,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(403, result.Error!.StatusCode);
        Assert.Empty(repository.StoredPlans);
    }

    private const string ClientId = "507f1f77bcf86cd799439011";

    private static CreateMealPlanHandler IdHandler(FakeMealPlanRepository repository, INutritionAccessPolicy? access = null) =>
        new(repository, new FakeFoodRepository("f1"), access ?? AllowAll(), new CreateMealPlanCommandValidator());

    private static CreateMealPlanCommand WithId(string? id, int? target = null) =>
        new("Plan", [new MealPlanItemInputDto("lunch", "f1", 100m)], target, id);

    [Fact]
    public async Task HandleAsync_WithClientId_UsesItAndResendDoesNotDuplicate()
    {
        var repository = new FakeMealPlanRepository();
        var handler = IdHandler(repository);

        var first = await handler.HandleAsync(WithId(ClientId), 7, default);
        var second = await handler.HandleAsync(WithId(ClientId), 7, default);

        Assert.Equal(ClientId, first.Value!.Id);
        Assert.Equal(ClientId, second.Value!.Id);
        Assert.Single(repository.StoredPlans);
    }

    [Fact]
    public async Task HandleAsync_WithoutId_GeneratesOne()
    {
        var result = await IdHandler(new FakeMealPlanRepository()).HandleAsync(WithId(null), 7, default);

        Assert.Matches("^[0-9a-f]{24}$", result.Value!.Id);
    }

    [Fact]
    public async Task HandleAsync_IdOwnedByAnotherUser_Returns409()
    {
        var repository = new FakeMealPlanRepository();
        var handler = IdHandler(repository);
        await handler.HandleAsync(WithId(ClientId), 7, default);

        var result = await handler.HandleAsync(WithId(ClientId), 8, default);

        Assert.Equal(409, result.Error!.StatusCode);
        Assert.Single(repository.StoredPlans);
    }

    [Fact]
    public async Task HandleAsync_IdUsedForAnotherTarget_Returns409()
    {
        var repository = new FakeMealPlanRepository();
        var handler = IdHandler(repository);
        await handler.HandleAsync(WithId(ClientId, target: 9), 7, default);

        Assert.Equal(409, (await handler.HandleAsync(WithId(ClientId, target: 10), 7, default)).Error!.StatusCode);
        Assert.Equal(409, (await handler.HandleAsync(WithId(ClientId), 7, default)).Error!.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_InvalidId_Returns400()
    {
        var result = await IdHandler(new FakeMealPlanRepository()).HandleAsync(WithId("not-an-objectid"), 7, default);

        Assert.Equal(400, result.Error!.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_ExistingIdOfAnotherUser_StillChecksTargetAuthorizationFirst()
    {
        var repository = new FakeMealPlanRepository();
        await IdHandler(repository).HandleAsync(WithId(ClientId), 9, default);
        var access = new Mock<INutritionAccessPolicy>();
        access.Setup(a => a.CanManageNutritionForAsync(7, 9, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await IdHandler(repository, access.Object).HandleAsync(WithId(ClientId, target: 9), 7, default);

        Assert.Equal(403, result.Error!.StatusCode);
    }

    private static INutritionAccessPolicy AllowAll()
    {
        var access = new Mock<INutritionAccessPolicy>();
        access.Setup(a => a.CanManageNutritionForAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        return access.Object;
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

        public Task SetExclusiveActiveAsync(int userId, string? planId, DateTime nowUtc, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeFoodRepository(string foodId) : IFoodRepository
    {
        public Task CreateAsync(FoodDocument food, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<FoodDocument?> GetByIdAsync(string id, CancellationToken cancellationToken) =>
            Task.FromResult(id == foodId ? new FoodDocument { Id = foodId, Name = "Rice", MacrosPer100 = new() { Kcal = 100, ProteinG = 2, CarbG = 20, FatG = 1 } } : null);

        public Task<FoodDocument?> GetByIdIncludingDeletedAsync(string id, CancellationToken cancellationToken) =>
            GetByIdAsync(id, cancellationToken);

        public Task<FoodDocument?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken) => Task.FromResult<FoodDocument?>(null);

        public Task<(IReadOnlyList<FoodDocument> Items, string? NextCursor)> SearchAsync(string query, int pageSize, string? cursor, string? category, CancellationToken cancellationToken) =>
            Task.FromResult<(IReadOnlyList<FoodDocument>, string?)>(([], null));

        public Task ApplyApprovedOverrideAsync(FoodOverrideDocument overrideDocument, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<bool> SoftDeleteAsync(string id, int deletedByUserId, DateTime deletedAtUtc, CancellationToken cancellationToken) =>
            Task.FromResult(false);
    }
}
