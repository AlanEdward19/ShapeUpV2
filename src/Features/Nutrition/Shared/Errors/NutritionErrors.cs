using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.Shared.Errors;

public static class NutritionErrors
{
    public static Error FoodBarcodeAlreadyExists(string barcode) =>
        CommonErrors.Conflict($"A food with barcode '{barcode}' already exists.");

    public static Error FoodNotFoundByBarcode(string barcode) =>
        CommonErrors.NotFound($"Food not found for barcode '{barcode}'.");
}
