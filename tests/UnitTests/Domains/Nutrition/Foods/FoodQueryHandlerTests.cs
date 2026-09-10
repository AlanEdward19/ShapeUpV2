using ShapeUp.Features.Nutrition.Foods.GetFoodByBarcode;
using ShapeUp.Features.Nutrition.Foods.SearchFoods;
using ShapeUp.Features.Nutrition.Shared.Abstractions;
using ShapeUp.Features.Nutrition.Shared.Documents;
using ShapeUp.Features.Nutrition.Shared.ValueObjects;

namespace UnitTests.Domains.Nutrition.Foods;

public class SearchFoodsHandlerTests
{
    private readonly Mock<IFoodRepository> _repository = new();

    [Fact]
    public async Task HandleAsync_WhenQueryMatchesCaseInsensitive_ReturnsMatchingFood()
    {
        var food = new FoodDocument
        {
            Id = "abc",
            Name = "Grilled Salmon",
            MacrosPer100 = new MacroValueObject { Kcal = 200, ProteinG = 20, CarbG = 0, FatG = 12 },
            CreatedByUserId = 1,
            CreatedAtUtc = DateTime.UtcNow
        };

        _repository
            .Setup(x => x.SearchAsync("SALMON", 20, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new[] { food }, (string?)null));

        var sut = new SearchFoodsHandler(_repository.Object, new SearchFoodsQueryValidator());

        var result = await sut.HandleAsync(new SearchFoodsQuery("SALMON", null, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal(food.Id, result.Value.Items[0].Id);
    }

    [Fact]
    public async Task HandleAsync_WhenQueryIsEmpty_ReturnsPageWithoutValidationFailure()
    {
        _repository
            .Setup(x => x.SearchAsync(string.Empty, 20, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Array.Empty<FoodDocument>(), (string?)null));

        var sut = new SearchFoodsHandler(_repository.Object, new SearchFoodsQueryValidator());

        var result = await sut.HandleAsync(new SearchFoodsQuery(string.Empty, null, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!.Items);
        Assert.Null(result.Value.NextCursor);
    }
}

public class GetFoodByBarcodeHandlerTests
{
    private readonly Mock<IFoodRepository> _repository = new();

    [Fact]
    public async Task HandleAsync_WhenBarcodeExists_ReturnsFood()
    {
        const string barcode = "7895556667778";
        var food = new FoodDocument
        {
            Id = "food-id",
            Name = "Barcode Food",
            Barcode = barcode,
            MacrosPer100 = new MacroValueObject { Kcal = 100, ProteinG = 10, CarbG = 20, FatG = 5 },
            CreatedByUserId = 1,
            CreatedAtUtc = DateTime.UtcNow
        };

        _repository
            .Setup(x => x.GetByBarcodeAsync(barcode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(food);

        var sut = new GetFoodByBarcodeHandler(_repository.Object, new GetFoodByBarcodeQueryValidator());

        var result = await sut.HandleAsync(new GetFoodByBarcodeQuery(barcode), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(food.Id, result.Value!.Id);
        Assert.Equal(barcode, result.Value.Barcode);
    }

    [Fact]
    public async Task HandleAsync_WhenBarcodeDoesNotExist_ReturnsNotFound()
    {
        const string barcode = "7899998887776";
        _repository
            .Setup(x => x.GetByBarcodeAsync(barcode, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FoodDocument?)null);

        var sut = new GetFoodByBarcodeHandler(_repository.Object, new GetFoodByBarcodeQueryValidator());

        var result = await sut.HandleAsync(new GetFoodByBarcodeQuery(barcode), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("not_found", result.Error!.Code);
        Assert.Contains(barcode, result.Error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleAsync_AcceptsManualBarcodeStringWithoutSpecialHandling()
    {
        const string manualBarcode = "manual-entry-123";
        _repository
            .Setup(x => x.GetByBarcodeAsync(manualBarcode, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FoodDocument?)null);

        var sut = new GetFoodByBarcodeHandler(_repository.Object, new GetFoodByBarcodeQueryValidator());

        var result = await sut.HandleAsync(new GetFoodByBarcodeQuery(manualBarcode), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("not_found", result.Error!.Code);
        _repository.Verify(x => x.GetByBarcodeAsync(manualBarcode, It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class GetFoodByBarcodeQueryValidatorTests
{
    private readonly GetFoodByBarcodeQueryValidator _validator = new();

    [Fact]
    public async Task Validate_WhenBarcodeIsEmpty_HasError()
    {
        var result = await _validator.ValidateAsync(new GetFoodByBarcodeQuery(string.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Barcode");
    }
}
