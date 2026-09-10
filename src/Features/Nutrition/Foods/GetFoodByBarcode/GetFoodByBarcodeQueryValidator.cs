using FluentValidation;

namespace ShapeUp.Features.Nutrition.Foods.GetFoodByBarcode;

public class GetFoodByBarcodeQueryValidator : AbstractValidator<GetFoodByBarcodeQuery>
{
    public GetFoodByBarcodeQueryValidator()
    {
        RuleFor(x => x.Barcode)
            .NotEmpty()
            .WithMessage("Barcode is required.")
            .MaximumLength(64);
    }
}
