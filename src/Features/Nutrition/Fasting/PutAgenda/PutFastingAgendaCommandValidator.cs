using FluentValidation;
using ShapeUp.Features.Nutrition.Fasting.Shared;

namespace ShapeUp.Features.Nutrition.Fasting.PutAgenda;

public sealed class PutFastingAgendaCommandValidator : AbstractValidator<PutFastingAgendaCommand>
{
    public PutFastingAgendaCommandValidator()
    {
        RuleFor(x => x.Protocol)
            .NotEmpty()
            .Must(p => FastingClockCalculator.ParsePresetProtocol(p) is not null
                || string.Equals(p, "custom", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Protocol must be a preset (14:10, 16:8, 18:6, 20:4) or custom.");

        RuleFor(x => x.FastHours)
            .NotNull()
            .When(x => string.Equals(x.Protocol, "custom", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Fast hours are required for a custom protocol.");

        RuleFor(x => x.FastHours)
            .InclusiveBetween(12, 23)
            .When(x => string.Equals(x.Protocol, "custom", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Fast hours must be between 12 and 23.");

        RuleFor(x => x.FastHours)
            .Null()
            .When(x => FastingClockCalculator.ParsePresetProtocol(x.Protocol) is not null)
            .WithMessage("Fast hours must not be set for preset protocols.");

        RuleFor(x => x.EatingStartMinutes)
            .InclusiveBetween(0, 1410)
            .Must(minutes => minutes % 30 == 0)
            .WithMessage("Eating start must be on a 30-minute grid between 00:00 and 23:30.");

        RuleFor(x => x.TimeZone)
            .NotEmpty()
            .Must(BeValidIanaTimeZone)
            .WithMessage("Time zone must be a valid IANA identifier.");
    }

    private static bool BeValidIanaTimeZone(string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
            return false;

        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(timeZoneId.Trim());
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            return false;
        }
    }
}
