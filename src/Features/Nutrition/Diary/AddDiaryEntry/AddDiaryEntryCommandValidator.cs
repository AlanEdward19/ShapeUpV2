using FluentValidation;

namespace ShapeUp.Features.Nutrition.Diary.AddDiaryEntry;

public class AddDiaryEntryCommandValidator : AbstractValidator<AddDiaryEntryCommand>
{
    private static readonly string[] AllowedMealSlots = ["breakfast", "lunch", "dinner", "snack"];

    public AddDiaryEntryCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().MaximumLength(24);
        RuleFor(x => x.Date).NotEmpty();
        RuleFor(x => x.MealSlot)
            .NotEmpty()
            .Must(slot => AllowedMealSlots.Contains(slot, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Meal slot must be breakfast, lunch, dinner, or snack.");
        RuleFor(x => x.FoodId).NotEmpty().MaximumLength(24);
        RuleFor(x => x.QuantityGramsOrMl).GreaterThan(0);
    }
}
