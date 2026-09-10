using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.Shared.Errors;

public static class NutritionErrors
{
    public static Error FoodBarcodeAlreadyExists(string barcode) =>
        CommonErrors.Conflict($"A food with barcode '{barcode}' already exists.");

    public static Error FoodNotFoundByBarcode(string barcode) =>
        CommonErrors.NotFound($"Food not found for barcode '{barcode}'.");

    public static Error FoodNotFound(string foodId) =>
        CommonErrors.NotFound($"Food '{foodId}' was not found.");

    public static Error FoodOverrideNotFound(string foodId) =>
        CommonErrors.NotFound($"No personal override exists for food '{foodId}'.");

    public static Error ModerationRequestNotFound(string requestId) =>
        CommonErrors.NotFound($"Food moderation request '{requestId}' was not found.");

    public static Error ModerationRequestAlreadyDecided(string requestId) =>
        CommonErrors.Conflict($"Food moderation request '{requestId}' has already been decided.");

    public static Error WeightNotRegistered() =>
        CommonErrors.Validation("At least one weight register is required before completing onboarding.");
}
