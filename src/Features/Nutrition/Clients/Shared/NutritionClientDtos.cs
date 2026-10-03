namespace ShapeUp.Features.Nutrition.Clients.Shared;

public record NutritionClientResponse(int ClientUserId, string? Name, DateTime StartedAtUtc);

public record InviteNutritionClientResponse(string Token, DateTime ExpiresAtUtc);

public record AcceptNutritionInviteCommand(string Token);

public record AcceptNutritionInviteResponse(int NutritionistUserId, DateTime StartedAtUtc);
