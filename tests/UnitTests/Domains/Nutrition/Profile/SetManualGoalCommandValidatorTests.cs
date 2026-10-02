using ShapeUp.Features.Nutrition.Profile.SetManualGoal;
using ShapeUp.Features.Nutrition.Profile.Shared.ViewModels;

namespace UnitTests.Domains.Nutrition.Profile;

public class SetManualGoalCommandValidatorTests
{
    private readonly SetManualGoalCommandValidator _validator = new();

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(2500)]
    [InlineData(10000)]
    public async Task Validate_WhenWaterMlIsAbsentOrInRange_IsValid(int? waterMl)
    {
        var command = new SetManualGoalCommand(new MacroGoalDto(2200, 150, 220, 70, waterMl));

        var result = await _validator.ValidateAsync(command);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(10001)]
    public async Task Validate_WhenWaterMlIsOutOfRange_ReturnsValidationError(int waterMl)
    {
        var command = new SetManualGoalCommand(new MacroGoalDto(2200, 150, 220, 70, waterMl));

        var result = await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName.EndsWith(nameof(MacroGoalDto.WaterMl)));
    }
}
