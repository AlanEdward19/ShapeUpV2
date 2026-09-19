namespace ShapeUp.Features.Nutrition.Fasting.Shared.ViewModels;

public sealed record FastingAgendaDto(
    string Protocol,
    int FastHours,
    int EatHours,
    int EatingStartMinutes,
    string TimeZone,
    DateTime UpdatedAtUtc);

public sealed record FastingRecommendationDto(string Protocol, int FastHours);

public sealed record FastingOverrideDto(
    Guid Id,
    string Status,
    string Protocol,
    int FastHours,
    int EatHours,
    DateTime StartedAtUtc,
    DateTime FastEndsAtUtc,
    DateTime? EatEndsAtUtc);

public sealed record FastingClockDto(string Status, DateTime? BoundaryAt, string? Source);

public sealed record FastingSnapshotResponse(
    FastingAgendaDto? Agenda,
    FastingOverrideDto? Override,
    FastingRecommendationDto? Recommendation,
    FastingClockDto Clock);

public sealed record PutFastingAgendaResponse(FastingAgendaDto Agenda);

public sealed record FastingHistoryItemDto(
    Guid Id,
    DateTime StartedAtUtc,
    string Protocol,
    string Outcome,
    int FastingDurationSeconds);
