using FluentValidation;

namespace ShapeUp.Features.Nutrition.Diary.GetDiaryDay;

public class GetDiaryDayQueryValidator : AbstractValidator<GetDiaryDayQuery>
{
    public GetDiaryDayQueryValidator()
    {
        RuleFor(x => x.Date).NotEmpty();
    }
}
