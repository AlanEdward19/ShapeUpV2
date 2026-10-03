using FluentValidation;

namespace ShapeUp.Features.Nutrition.Comments.AddComment;

public class AddDiaryCommentCommandValidator : AbstractValidator<AddDiaryCommentCommand>
{
    public AddDiaryCommentCommandValidator()
    {
        RuleFor(x => x.Date).NotEqual(default(DateOnly));
        RuleFor(x => x.Text).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.EntryId).MaximumLength(24);
    }
}
