using ShapeUp.Features.Gamification.Shared.Enums;

namespace ShapeUp.Features.Gamification.Shared.AntiCheat;

public sealed record AntiCheatResult(ActivityClassification Classification, string? Reason);
