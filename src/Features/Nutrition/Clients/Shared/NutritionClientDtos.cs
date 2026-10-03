namespace ShapeUp.Features.Nutrition.Clients.Shared;

public record NutritionClientResponse(int ClientUserId, string? Name, DateTime StartedAtUtc);

public record InviteNutritionClientResponse(string Token, DateTime ExpiresAtUtc);

public record AcceptNutritionInviteCommand(string Token);

public record AcceptNutritionInviteResponse(int NutritionistUserId, DateTime StartedAtUtc);

public record NutritionistResponse(int NutritionistUserId, string? Name, DateTime StartedAtUtc);

/// <summary>
/// Adherence of one client over the last <paramref name="Days"/> days (UTC, today included).
/// A day is within goal when the logged macros are inside the tolerance of the prescribed goal
/// (same rule that feeds the gamification streak). Averages cover only the days with a diary record.
/// </summary>
public record NutritionClientAdherenceResponse(
    int ClientUserId,
    string? Name,
    int Days,
    int DaysLogged,
    int DaysWithinGoal,
    Diary.Shared.ViewModels.MacroTotalsDto? Prescribed,
    Diary.Shared.ViewModels.MacroTotalsDto? AverageConsumed,
    DateOnly? LastLoggedDate);
