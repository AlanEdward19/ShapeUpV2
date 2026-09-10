using ShapeUp.Features.Nutrition.Foods.DeleteFood;
using ShapeUp.Features.Nutrition.Shared.Abstractions;

namespace UnitTests.Domains.Nutrition.Foods;

public class DeleteFoodHandlerTests
{
    private readonly Mock<IFoodRepository> _foodRepository = new();
    private readonly DeleteFoodHandler _handler;

    public DeleteFoodHandlerTests()
    {
        _handler = new DeleteFoodHandler(_foodRepository.Object);
    }

    [Fact]
    public async Task HandleAsync_WhenFoodAlreadyDeleted_ReturnsSuccessWithoutError()
    {
        const string foodId = "deleted-food";
        const int adminUserId = 99;

        _foodRepository
            .Setup(x => x.SoftDeleteAsync(foodId, adminUserId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _handler.HandleAsync(new DeleteFoodCommand(foodId), adminUserId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        _foodRepository.Verify(
            x => x.SoftDeleteAsync(foodId, adminUserId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenFoodExists_SoftDeletesAndReturnsSuccess()
    {
        const string foodId = "existing-food";
        const int adminUserId = 42;

        _foodRepository
            .Setup(x => x.SoftDeleteAsync(foodId, adminUserId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _handler.HandleAsync(new DeleteFoodCommand(foodId), adminUserId, CancellationToken.None);

        Assert.True(result.IsSuccess);
    }
}
