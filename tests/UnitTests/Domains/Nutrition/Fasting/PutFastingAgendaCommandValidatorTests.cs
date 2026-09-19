using ShapeUp.Features.Nutrition.Fasting.PutAgenda;

namespace UnitTests.Domains.Nutrition.Fasting;

public sealed class PutFastingAgendaCommandValidatorTests
{
    private readonly PutFastingAgendaCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WhenProtocolIsInvalid_ReturnsProtocolPropertyError()
    {
        var command = new PutFastingAgendaCommand("8:16", 720, FastingTestSupport.SaoPaulo, null);

        var result = await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(PutFastingAgendaCommand.Protocol));
    }

    [Fact]
    public async Task Validate_WhenEatingStartIsNotOnGrid_ReturnsEatingStartMinutesPropertyError()
    {
        var command = new PutFastingAgendaCommand("16:8", 715, FastingTestSupport.SaoPaulo, null);

        var result = await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(PutFastingAgendaCommand.EatingStartMinutes));
    }

    [Fact]
    public async Task Validate_WhenTimeZoneIsInvalid_ReturnsTimeZonePropertyError()
    {
        var command = new PutFastingAgendaCommand("16:8", 720, "Not/A_Zone", null);

        var result = await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(PutFastingAgendaCommand.TimeZone));
    }

    [Fact]
    public async Task Validate_WhenCustomFastHoursIsTooLow_ReturnsFastHoursPropertyError()
    {
        var command = new PutFastingAgendaCommand("custom", 720, FastingTestSupport.SaoPaulo, 8);

        var result = await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(PutFastingAgendaCommand.FastHours));
    }
}
