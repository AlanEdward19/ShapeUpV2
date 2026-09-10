using ShapeUp.Features.Nutrition.Foods.Shared.ViewModels;

namespace ShapeUp.Features.Nutrition.Moderation.Shared.ViewModels;

public record PendingModerationResponse(
    string RequestId,
    string FoodId,
    string FoodName,
    int RequestedByUserId,
    DateTime CreatedAtUtc,
    MacroResponseDto PublicMacros,
    MicroResponseDto? PublicMicros,
    MacroResponseDto ProposedMacros,
    MicroResponseDto? ProposedMicros);

public record DecideModerationResponse(
    string RequestId,
    string Status,
    DateTime DecidedAtUtc);
