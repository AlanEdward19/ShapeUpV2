using ShapeUp.Features.Nutrition.Foods.CreateFood;
using ShapeUp.Features.Nutrition.Foods.Shared.ViewModels;
using ShapeUp.Features.Nutrition.Shared.Abstractions;
using ShapeUp.Features.Nutrition.Shared.Documents;
using ShapeUp.Features.Nutrition.Shared.ValueObjects;

namespace UnitTests.Domains.Nutrition.Foods;

public class CreateFoodHandlerTests
{
    private readonly Mock<IFoodRepository> _repository = new();

    private static CreateFoodCommand ValidCommand(string? barcode = null) =>
        new(
            "Brown Rice",
            barcode,
            new MacroInputDto(111, 3, 23, 1),
            null,
            null);

    [Fact]
    public async Task HandleAsync_WhenMicrosAreOmitted_CreatesFoodWithNullMicros()
    {
        FoodDocument? persisted = null;
        _repository
            .Setup(x => x.CreateAsync(It.IsAny<FoodDocument>(), It.IsAny<CancellationToken>()))
            .Callback<FoodDocument, CancellationToken>((food, _) => persisted = food)
            .Returns(Task.CompletedTask);

        var sut = new CreateFoodHandler(_repository.Object, new CreateFoodCommandValidator());

        var result = await sut.HandleAsync(ValidCommand(), 42, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(persisted);
        Assert.Null(persisted!.MicrosPer100);
        Assert.Equal("Brown Rice", result.Value!.Name);
        Assert.Equal(42, result.Value.CreatedByUserId);
    }

    [Fact]
    public async Task HandleAsync_WhenBarcodeAlreadyExists_ReturnsConflict()
    {
        const string barcode = "7890001112223";
        _repository
            .Setup(x => x.GetByBarcodeAsync(barcode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FoodDocument
            {
                Id = "existing",
                Name = "Existing Food",
                Barcode = barcode,
                MacrosPer100 = new MacroValueObject { Kcal = 1, ProteinG = 1, CarbG = 1, FatG = 1 },
                CreatedByUserId = 1,
                CreatedAtUtc = DateTime.UtcNow
            });

        var sut = new CreateFoodHandler(_repository.Object, new CreateFoodCommandValidator());

        var result = await sut.HandleAsync(ValidCommand(barcode), 42, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("conflict", result.Error!.Code);
        Assert.Contains("already exists", result.Error.Message, StringComparison.OrdinalIgnoreCase);
        _repository.Verify(x => x.CreateAsync(It.IsAny<FoodDocument>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenBarcodeIsNew_CreatesFood()
    {
        const string barcode = "7890003334445";
        _repository
            .Setup(x => x.GetByBarcodeAsync(barcode, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FoodDocument?)null);
        _repository
            .Setup(x => x.CreateAsync(It.IsAny<FoodDocument>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = new CreateFoodHandler(_repository.Object, new CreateFoodCommandValidator());

        var result = await sut.HandleAsync(ValidCommand(barcode), 42, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(barcode, result.Value!.Barcode);
        _repository.Verify(x => x.CreateAsync(It.Is<FoodDocument>(f => f.Barcode == barcode), It.IsAny<CancellationToken>()), Times.Once);
    }
}
