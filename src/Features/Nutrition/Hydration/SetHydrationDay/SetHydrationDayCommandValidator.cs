using FluentValidation;

namespace ShapeUp.Features.Nutrition.Hydration.SetHydrationDay;

public class SetHydrationDayCommandValidator : AbstractValidator<SetHydrationDayCommand>
{
    public const int MaxDailyMl = 10000;

    public SetHydrationDayCommandValidator()
    {
        RuleFor(x => x.Date)
            .Must(date => date >= new DateOnly(2000, 1, 1) && date <= DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(1)))
            .WithMessage("Date must be a valid day, at most tomorrow.");

        RuleFor(x => x.TotalMl).InclusiveBetween(0, MaxDailyMl);
    }
}
