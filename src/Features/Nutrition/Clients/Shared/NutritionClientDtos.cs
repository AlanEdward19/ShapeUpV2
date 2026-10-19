namespace ShapeUp.Features.Nutrition.Clients.Shared;

public record NutritionClientResponse(int ClientUserId, string? Name, DateTime StartedAtUtc);

/// <summary>
/// <paramref name="InviteId"/> lets the nutritionist revoke the invite before it is used.
/// <paramref name="InviteUrl"/> opens the app (or the sign-up page) with the token filled in; use it for QR and sharing.
/// <paramref name="EmailSent"/> is false when no e-mail was requested or the provider failed: the invite exists either way
/// and can still be handed over by link/QR.
/// </summary>
public record InviteNutritionClientResponse(
    string Token,
    DateTime ExpiresAtUtc,
    int InviteId,
    string? InviteUrl = null,
    string? ClientEmail = null,
    bool EmailSent = false,
    string? EmailError = null);

/// <summary>
/// An invite as listed to the nutritionist (the token is never returned again).
/// <paramref name="Status"/> is Pending, Accepted, Revoked or Expired; a pending invite past its deadline is reported as Expired.
/// </summary>
/// <summary>Optional body of the invite request: how long the invite lives (default and limits in <c>InviteRules</c>).</summary>
public record InviteNutritionClientCommand(int? ExpiresInHours, string? ClientEmail = null);

public record NutritionInviteResponse(int InviteId, DateTime CreatedAtUtc, DateTime ExpiresAtUtc, string Status, string? ClientEmail = null);

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
