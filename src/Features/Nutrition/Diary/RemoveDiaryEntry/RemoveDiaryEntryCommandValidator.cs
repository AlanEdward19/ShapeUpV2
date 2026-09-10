using FluentValidation;

namespace ShapeUp.Features.Nutrition.Diary.RemoveDiaryEntry;

public class RemoveDiaryEntryCommandValidator : AbstractValidator<RemoveDiaryEntryCommand>
{
    public RemoveDiaryEntryCommandValidator()
    {
        RuleFor(x => x.EntryId).NotEmpty().MaximumLength(24);
        RuleFor(x => x.Date).NotEmpty();
    }
}
