using FluentValidation;

namespace ShapeUp.Features.Nutrition.Comments.AddComment;

public class AddDiaryCommentCommandValidator : AbstractValidator<AddDiaryCommentCommand>
{
    public AddDiaryCommentCommandValidator()
    {
        RuleFor(x => x.Date).NotEqual(default(DateOnly));
        RuleFor(x => x.Text).NotEmpty().MaximumLength(1000)
            .Must(t => !ShapeUp.Shared.Safety.ObjectionableText.Contains(t)).WithMessage("Text contains language that is not allowed.");
        RuleFor(x => x.EntryId).MaximumLength(24);
    }
}
