using ShapeUp.Features.Nutrition.Foods.CreateFood;
using ShapeUp.Features.Nutrition.Foods.Shared.ViewModels;

namespace UnitTests.Domains.Nutrition.Foods;

public class CreateFoodCommandValidatorTests
{
    private readonly CreateFoodCommandValidator _validator = new();

    private static CreateFoodCommand ValidCommand(string? barcode = null, MicroInputDto? micros = null) =>
        new(
            "Grilled Chicken",
            barcode,
            new MacroInputDto(165, 31, 0, 4),
            micros,
            null);

    [Fact]
    public async Task Validate_WhenNameAndMacrosArePresent_IsValid()
    {
        var result = await _validator.ValidateAsync(ValidCommand());

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validate_WhenMicrosAreOmitted_IsValid()
    {
        var result = await _validator.ValidateAsync(ValidCommand(micros: null));

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validate_WhenNameIsMissing_HasNameError()
    {
        var command = ValidCommand() with { Name = string.Empty };

        var result = await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
    }

    [Theory]
    [InlineData(-1, 10, 20, 5, "MacrosPer100.Kcal")]
    [InlineData(100, -1, 20, 5, "MacrosPer100.ProteinG")]
    [InlineData(100, 10, -1, 5, "MacrosPer100.CarbG")]
    [InlineData(100, 10, 20, -1, "MacrosPer100.FatG")]
    public async Task Validate_WhenAnyMacroIsNegative_HasMacroError(
        int kcal,
        int protein,
        int carb,
        int fat,
        string expectedProperty)
    {
        var command = ValidCommand() with
        {
            MacrosPer100 = new MacroInputDto(kcal, protein, carb, fat)
        };

        var result = await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == expectedProperty);
    }

    [Fact]
    public async Task Validate_WhenBarcodeIsProvided_IsValid()
    {
        var result = await _validator.ValidateAsync(ValidCommand(barcode: "7891234567890"));

        Assert.True(result.IsValid);
    }
}
