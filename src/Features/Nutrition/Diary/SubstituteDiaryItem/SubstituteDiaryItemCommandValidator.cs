using FluentValidation;

namespace ShapeUp.Features.Nutrition.Diary.SubstituteDiaryItem;

public class SubstituteDiaryItemCommandValidator : AbstractValidator<SubstituteDiaryItemCommand>
{
    public SubstituteDiaryItemCommandValidator()
    {
        RuleFor(x => x.Date).NotEmpty();
        RuleFor(x => x.EntryId).NotEmpty().MaximumLength(24);
        RuleFor(x => x.ReplacementFoodId).NotEmpty().MaximumLength(24);
        RuleFor(x => x.QuantityGramsOrMl).GreaterThan(0);
    }
}
