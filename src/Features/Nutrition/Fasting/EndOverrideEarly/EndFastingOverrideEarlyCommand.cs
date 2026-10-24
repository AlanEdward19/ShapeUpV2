namespace ShapeUp.Features.Nutrition.Fasting.EndOverrideEarly;

/// <summary>Kept for compatibility: same recording as <c>POST fasting/end-early</c>, limited to a manual override.</summary>
public sealed record EndFastingOverrideEarlyCommand(string? Reason = null);
