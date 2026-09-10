using FluentValidation;

namespace ShapeUp.Features.Nutrition.Foods.SearchFoods;

public class SearchFoodsQueryValidator : AbstractValidator<SearchFoodsQuery>
{
    public SearchFoodsQueryValidator()
    {
        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .When(x => x.PageSize.HasValue);
    }
}
