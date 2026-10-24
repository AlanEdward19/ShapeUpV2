using ShapeUp.Features.Nutrition.Fasting.Shared.ViewModels;
using ShapeUp.Features.Nutrition.Shared.Entities;

namespace ShapeUp.Features.Nutrition.Fasting.Shared;

public static class FastingMapper
{
    public const int MaxReasonLength = 280;
    public const string ClockSourceAgenda = "Agenda";
    public const string ClockSourceOverride = "Override";

    public static FastingAgendaDto ToAgendaDto(FastingAgenda entity) =>
        new(
            entity.Protocol!,
            entity.FastHours!.Value,
            entity.EatHours!.Value,
            entity.EatingStartMinutes!.Value,
            entity.TimeZone!,
            entity.UpdatedAtUtc);

    public static FastingRecommendationDto? ToRecommendationDto(FastingAgenda entity) =>
        entity.RecommendedProtocol is null || entity.RecommendedFastHours is null
            ? null
            : new FastingRecommendationDto(
                entity.RecommendedProtocol,
                entity.RecommendedFastHours.Value,
                EffectiveRecommendationStatus(entity),
                entity.RecommendedByUserId,
                entity.RecommendedAtUtc,
                entity.RecommendationDeclinedAtUtc,
                entity.RecommendationDeclineReason);

    /// <summary>Rows written before the lifecycle existed have no status: a saved agenda means it was adopted.</summary>
    public static string EffectiveRecommendationStatus(FastingAgenda entity) =>
        entity.RecommendationStatus
        ?? (HasSavedAgenda(entity) ? FastingAgenda.RecommendationAdopted : FastingAgenda.RecommendationPending);

    /// <summary>Trims the reason; empty becomes null.</summary>
    public static string? NormalizeReason(string? reason)
    {
        var trimmed = reason?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    public static FastingOverrideDto ToOverrideDto(FastingOverride entity) =>
        new(
            entity.Id,
            entity.Status,
            entity.Protocol,
            entity.FastHours,
            entity.EatHours,
            entity.StartedAtUtc,
            entity.FastEndsAtUtc,
            entity.EatEndsAtUtc);

    public static FastingClockDto ToClockDto(FastingClockState state, string? source) =>
        new(state.Status, state.BoundaryAt, source);

    public static bool HasSavedAgenda(FastingAgenda? entity) =>
        entity?.FastHours is not null
        && entity.EatHours is not null
        && entity.Protocol is not null
        && entity.EatingStartMinutes is not null
        && entity.TimeZone is not null;
}
