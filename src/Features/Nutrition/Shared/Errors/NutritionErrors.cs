using Microsoft.AspNetCore.Http;
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

    public static Error DiaryEntryNotFound(string entryId) =>
        CommonErrors.NotFound($"Diary entry '{entryId}' was not found.");

    public static Error MealPlanNotFound(string mealPlanId) =>
        CommonErrors.NotFound($"Meal plan '{mealPlanId}' was not found.");

    public static Error FastingDisabled() =>
        new("nutrition.fasting.disabled", "Intermittent fasting is not available.", StatusCodes.Status404NotFound);

    public static Error FastingAgendaRequired() =>
        CommonErrors.Validation("A saved fasting agenda with protocol is required.");

    public static Error FastingOverrideAlreadyActive() =>
        CommonErrors.Conflict("An active fasting override already exists.");

    public static Error FastingNoActiveOverride() =>
        CommonErrors.Conflict("No active fasting override exists.");

    public static Error FastingEndEarlyWhileEating() =>
        CommonErrors.Conflict("End-early is not allowed while the override is in the eating phase.");
}
