using ShapeUp.Features.Nutrition.Foods.SetActiveFoodVersion;
using ShapeUp.Features.Nutrition.Shared.Abstractions;
using ShapeUp.Features.Nutrition.Shared.Documents;
using ShapeUp.Features.Nutrition.Shared.ValueObjects;

namespace UnitTests.Domains.Nutrition.Foods;

public class SetActiveFoodVersionHandlerTests
{
    private readonly Mock<IFoodRepository> _foodRepository = new();
    private readonly Mock<IFoodOverrideRepository> _overrideRepository = new();
    private readonly SetActiveFoodVersionHandler _handler;

    public SetActiveFoodVersionHandlerTests()
    {
        _handler = new SetActiveFoodVersionHandler(
            _foodRepository.Object,
            _overrideRepository.Object,
            new SetActiveFoodVersionCommandValidator());
    }

    [Fact]
    public async Task HandleAsync_WhenSwitchingToPublic_DeactivatesOverrideButKeepsDocument()
    {
        const int userId = 5;
        var food = new FoodDocument
        {
            Id = "food-switch",
            Name = "Pasta",
            MacrosPer100 = new MacroValueObject { Kcal = 131, ProteinG = 5, CarbG = 25, FatG = 1 },
            CreatedByUserId = 1,
            CreatedAtUtc = DateTime.UtcNow
        };
        var overrideDocument = new FoodOverrideDocument
        {
            Id = "override-switch",
            FoodId = food.Id,
            UserId = userId,
            MacrosPer100 = new MacroValueObject { Kcal = 150, ProteinG = 6, CarbG = 28, FatG = 2 },
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        _foodRepository.Setup(x => x.GetByIdAsync(food.Id, It.IsAny<CancellationToken>())).ReturnsAsync(food);
        _overrideRepository.Setup(x => x.GetForUserAsync(food.Id, userId, It.IsAny<CancellationToken>())).ReturnsAsync(overrideDocument);

        var result = await _handler.HandleAsync(
            food.Id,
            new SetActiveFoodVersionCommand(UsePersonalOverride: false),
            userId,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.IsPersonalOverride);
        Assert.Equal(131, result.Value.MacrosPer100.Kcal);

        _overrideRepository.Verify(
            x => x.SetActiveAsync(overrideDocument.Id, userId, false, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
