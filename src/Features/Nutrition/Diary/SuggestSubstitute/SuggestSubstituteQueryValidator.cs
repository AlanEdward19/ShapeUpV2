using FluentValidation;

namespace ShapeUp.Features.Nutrition.Diary.SuggestSubstitute;

public class SuggestSubstituteQueryValidator : AbstractValidator<SuggestSubstituteQuery>
{
    public SuggestSubstituteQueryValidator()
    {
        RuleFor(x => x.Date).NotEmpty();
        RuleFor(x => x.EntryId).NotEmpty().MaximumLength(24);
    }
}
