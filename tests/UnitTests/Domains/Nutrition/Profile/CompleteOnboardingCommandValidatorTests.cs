using ShapeUp.Features.Nutrition.Profile.CompleteOnboarding;

namespace UnitTests.Domains.Nutrition.Profile;

public class CompleteOnboardingCommandValidatorTests
{
    private readonly CompleteOnboardingCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WhenHeightIsOutOfRange_ReturnsValidationError()
    {
        var command = new CompleteOnboardingCommand(90, 30, "Male", "Moderate");

        var result = await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CompleteOnboardingCommand.HeightCm));
    }

    [Fact]
    public async Task Validate_WhenAgeIsOutOfRange_ReturnsValidationError()
    {
        var command = new CompleteOnboardingCommand(170, 5, "Male", "Moderate");

        var result = await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CompleteOnboardingCommand.Age));
    }
}
