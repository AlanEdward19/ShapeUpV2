namespace ShapeUp.Features.Nutrition.Meals;

/// <summary>
/// A meal the user defined (any name, optional weekdays and time, own position).
/// The diary links entries to it through <c>MealId</c>.
/// </summary>
public class UserMeal
{
    public string Id { get; set; } = null!;
    public int UserId { get; set; }
    public string Name { get; set; } = null!;

    /// <summary>"HH:mm" local time of the day; null means no reminder.</summary>
    public string? Time { get; set; }

    /// <summary>ISO weekdays (Monday = 1 ... Sunday = 7) as "1,3,5"; null/empty means every day.</summary>
    public string? WeekDays { get; set; }

    /// <summary>First day the meal applies to ("from the day it was created"); null means it also shows on past days.</summary>
    public DateOnly? StartDate { get; set; }

    /// <summary>Last day the meal applies to ("only today" = start and end on the same day); null means no end.</summary>
    public DateOnly? EndDate { get; set; }

    public int SortOrder { get; set; }
    public int? TargetProteinG { get; set; }
    public int? TargetCarbG { get; set; }
    public int? TargetFatG { get; set; }

    /// <summary>Planned foods, a JSON array owned by the client (opaque to the API).</summary>
    public string? ItemsJson { get; set; }
}
