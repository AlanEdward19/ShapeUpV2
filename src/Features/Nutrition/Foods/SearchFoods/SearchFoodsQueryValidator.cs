using FluentValidation;
using ShapeUp.Features.Nutrition.Shared;

namespace ShapeUp.Features.Nutrition.Foods.SearchFoods;

public class SearchFoodsQueryValidator : AbstractValidator<SearchFoodsQuery>
{
    public SearchFoodsQueryValidator()
    {
        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .When(x => x.PageSize.HasValue);

        RuleFor(x => x.Category)
            .Must(FoodCategories.IsValid)
            .WithMessage("Category must be 'Food' or 'Supplement'.")
            .When(x => !string.IsNullOrWhiteSpace(x.Category));
    }
}
