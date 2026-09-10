using FluentValidation;
using ShapeUp.Features.Nutrition.Foods.CreateFoodOverride;
using ShapeUp.Features.Nutrition.Foods.Shared.ViewModels;
using ShapeUp.Features.Nutrition.Shared.Abstractions;
using ShapeUp.Features.Nutrition.Shared.Documents;
using ShapeUp.Features.Nutrition.Shared.ValueObjects;

namespace UnitTests.Domains.Nutrition.Foods;

public class CreateFoodOverrideHandlerTests
{
    private readonly Mock<IFoodRepository> _foodRepository = new();
    private readonly Mock<IFoodOverrideRepository> _overrideRepository = new();
    private readonly Mock<IFoodModerationRepository> _moderationRepository = new();
    private readonly CreateFoodOverrideHandler _handler;

    public CreateFoodOverrideHandlerTests()
    {
        _handler = new CreateFoodOverrideHandler(
            _foodRepository.Object,
            _overrideRepository.Object,
            _moderationRepository.Object,
            new CreateFoodOverrideCommandValidator());
    }

    [Fact]
    public async Task HandleAsync_CreatesOverrideAndModerationWithoutChangingPublicFood()
    {
        const int userId = 7;
        var publicFood = new FoodDocument
        {
            Id = "food-public",
            Name = "Yogurt",
            MacrosPer100 = new MacroValueObject { Kcal = 60, ProteinG = 4, CarbG = 8, FatG = 2 },
            CreatedByUserId = 1,
            CreatedAtUtc = DateTime.UtcNow
        };

        _foodRepository
            .Setup(x => x.GetByIdAsync(publicFood.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(publicFood);
        _overrideRepository
            .Setup(x => x.GetForUserAsync(publicFood.Id, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FoodOverrideDocument?)null);

        var command = new CreateFoodOverrideCommand(
            publicFood.Id,
            new MacroInputDto(80, 6, 10, 3),
            null);

        var result = await _handler.HandleAsync(command, userId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.IsPersonalOverride);
        Assert.Equal(80, result.Value.MacrosPer100.Kcal);
        Assert.Equal(60, publicFood.MacrosPer100.Kcal);

        _overrideRepository.Verify(
            x => x.CreateAsync(
                It.Is<FoodOverrideDocument>(o =>
                    o.FoodId == publicFood.Id &&
                    o.UserId == userId &&
                    o.IsActive &&
                    o.MacrosPer100.Kcal == 80),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _moderationRepository.Verify(
            x => x.CreateAsync(
                It.Is<FoodModerationRequestDocument>(r =>
                    r.FoodId == publicFood.Id &&
                    r.RequestedByUserId == userId &&
                    r.Status == "Pending"),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _foodRepository.Verify(
            x => x.ApplyApprovedOverrideAsync(It.IsAny<FoodOverrideDocument>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
