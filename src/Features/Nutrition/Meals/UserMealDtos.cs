namespace ShapeUp.Features.Nutrition.Meals;

public record UserMealDto(
    string Id,
    string Name,
    string? Time,
    int[] WeekDays,
    int Order,
    int? TargetProteinG,
    int? TargetCarbG,
    int? TargetFatG,
    string? ItemsJson,
    DateOnly? StartDate = null,
    DateOnly? EndDate = null);

public record SaveUserMealBody(
    string Name,
    string? Time,
    int[]? WeekDays,
    int? Order,
    int? TargetProteinG,
    int? TargetCarbG,
    int? TargetFatG,
    string? ItemsJson,
    DateOnly? StartDate = null,
    DateOnly? EndDate = null);

/// <summary>One meal to create or change in a batch (its position comes from <see cref="BatchUserMealsBody.Order"/>).</summary>
public record BatchUserMealUpsert(
    string Id,
    string Name,
    string? Time,
    int[]? WeekDays,
    int? TargetProteinG,
    int? TargetCarbG,
    int? TargetFatG,
    string? ItemsJson,
    DateOnly? StartDate = null,
    DateOnly? EndDate = null);

/// <summary>
/// Everything that changed at once, applied in a single transaction: meals to create/change, meals to delete
/// (missing ids are ignored, so a retry is safe) and, optionally, the full order the meals should end up in.
/// </summary>
public record BatchUserMealsBody(
    BatchUserMealUpsert[]? Upserts,
    string[]? DeleteIds,
    string[]? Order);

internal static class UserMealMapper
{
    internal static UserMealDto ToDto(UserMeal meal) => new(
        meal.Id,
        meal.Name,
        meal.Time,
        ParseWeekDays(meal.WeekDays),
        meal.SortOrder,
        meal.TargetProteinG,
        meal.TargetCarbG,
        meal.TargetFatG,
        meal.ItemsJson,
        meal.StartDate,
        meal.EndDate);

    internal static void Apply(UserMeal meal, SaveUserMealBody body)
    {
        meal.Name = body.Name.Trim();
        meal.Time = string.IsNullOrWhiteSpace(body.Time) ? null : body.Time;
        meal.WeekDays = FormatWeekDays(body.WeekDays);
        meal.TargetProteinG = body.TargetProteinG;
        meal.TargetCarbG = body.TargetCarbG;
        meal.TargetFatG = body.TargetFatG;
        meal.ItemsJson = body.ItemsJson;
        meal.StartDate = body.StartDate;
        meal.EndDate = body.EndDate;
    }

    internal static int[] ParseWeekDays(string? raw) =>
        string.IsNullOrWhiteSpace(raw)
            ? []
            : raw.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(int.Parse)
                .ToArray();

    internal static string? FormatWeekDays(int[]? days) =>
        days is null || days.Length == 0
            ? null
            : string.Join(',', days.Distinct().OrderBy(d => d));
}
