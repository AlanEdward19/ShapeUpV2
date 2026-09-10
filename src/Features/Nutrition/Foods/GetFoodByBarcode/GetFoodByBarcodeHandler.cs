using FluentValidation;
using ShapeUp.Features.Nutrition.Foods.Shared;
using ShapeUp.Features.Nutrition.Foods.Shared.ViewModels;
using ShapeUp.Features.Nutrition.Shared.Abstractions;
using ShapeUp.Features.Nutrition.Shared.Errors;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.Foods.GetFoodByBarcode;

public class GetFoodByBarcodeHandler(
    IFoodRepository foodRepository,
    IFoodOverrideRepository foodOverrideRepository,
    IValidator<GetFoodByBarcodeQuery> validator)
{
    public async Task<Result<FoodResponse>> HandleAsync(
        GetFoodByBarcodeQuery query,
        int? userId,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(query, cancellationToken);
        if (!validation.IsValid)
            return Result<FoodResponse>.Failure(CommonErrors.Validation(string.Join("; ", validation.Errors.Select(x => x.ErrorMessage))));

        var food = await foodRepository.GetByBarcodeAsync(query.Barcode.Trim(), cancellationToken);
        if (food is null)
            return Result<FoodResponse>.Failure(NutritionErrors.FoodNotFoundByBarcode(query.Barcode.Trim()));

        var activeOverride = userId.HasValue
            ? await foodOverrideRepository.GetActiveForUserAsync(food.Id, userId.Value, cancellationToken)
            : null;

        return Result<FoodResponse>.Success(FoodVersionResolver.Resolve(food, activeOverride));
    }
}
