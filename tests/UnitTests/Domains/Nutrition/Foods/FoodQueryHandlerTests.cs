using ShapeUp.Features.Nutrition.Foods.GetFoodByBarcode;
using ShapeUp.Features.Nutrition.Foods.SearchFoods;
using ShapeUp.Features.Nutrition.Shared.Abstractions;
using ShapeUp.Features.Nutrition.Shared.Documents;
using ShapeUp.Features.Nutrition.Shared.ValueObjects;

namespace UnitTests.Domains.Nutrition.Foods;

public class SearchFoodsHandlerTests
{
    private readonly Mock<IFoodRepository> _repository = new();
    private readonly Mock<IFoodOverrideRepository> _overrideRepository = new();

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

        _overrideRepository
            .Setup(x => x.GetActiveForUserByFoodIdsAsync(42, It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<FoodOverrideDocument>());

        var sut = new SearchFoodsHandler(_repository.Object, _overrideRepository.Object, new SearchFoodsQueryValidator());

        var result = await sut.HandleAsync(new SearchFoodsQuery("SALMON", null, null), 42, CancellationToken.None);

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

        var sut = new SearchFoodsHandler(_repository.Object, _overrideRepository.Object, new SearchFoodsQueryValidator());

        var result = await sut.HandleAsync(new SearchFoodsQuery(string.Empty, null, null), null, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!.Items);
        Assert.Null(result.Value.NextCursor);
    }

    [Fact]
    public async Task HandleAsync_WhenUserHasActiveOverride_ReturnsOverrideValuesInSearch()
    {
        const int userId = 42;
        var food = new FoodDocument
        {
            Id = "food-override-search",
            Name = "Searchable Oats",
            MacrosPer100 = new MacroValueObject { Kcal = 100, ProteinG = 4, CarbG = 18, FatG = 2 },
            CreatedByUserId = 1,
            CreatedAtUtc = DateTime.UtcNow
        };

        _repository
            .Setup(x => x.SearchAsync("oats", 20, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new[] { food }, (string?)null));
        _overrideRepository
            .Setup(x => x.GetActiveForUserByFoodIdsAsync(userId, It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new FoodOverrideDocument
                {
                    Id = "override-search",
                    FoodId = food.Id,
                    UserId = userId,
                    MacrosPer100 = new MacroValueObject { Kcal = 130, ProteinG = 6, CarbG = 20, FatG = 3 },
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow
                }
            });

        var sut = new SearchFoodsHandler(_repository.Object, _overrideRepository.Object, new SearchFoodsQueryValidator());
        var result = await sut.HandleAsync(new SearchFoodsQuery("oats", null, null), userId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.True(result.Value.Items[0].IsPersonalOverride);
        Assert.Equal(130, result.Value.Items[0].MacrosPer100.Kcal);
    }
}

public class GetFoodByBarcodeHandlerTests
{
    private readonly Mock<IFoodRepository> _repository = new();
    private readonly Mock<IFoodOverrideRepository> _overrideRepository = new();

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

        var sut = new GetFoodByBarcodeHandler(_repository.Object, _overrideRepository.Object, new GetFoodByBarcodeQueryValidator());

        var result = await sut.HandleAsync(new GetFoodByBarcodeQuery(barcode), null, CancellationToken.None);

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

        var sut = new GetFoodByBarcodeHandler(_repository.Object, _overrideRepository.Object, new GetFoodByBarcodeQueryValidator());

        var result = await sut.HandleAsync(new GetFoodByBarcodeQuery(barcode), null, CancellationToken.None);

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

        var sut = new GetFoodByBarcodeHandler(_repository.Object, _overrideRepository.Object, new GetFoodByBarcodeQueryValidator());

        var result = await sut.HandleAsync(new GetFoodByBarcodeQuery(manualBarcode), null, CancellationToken.None);

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
