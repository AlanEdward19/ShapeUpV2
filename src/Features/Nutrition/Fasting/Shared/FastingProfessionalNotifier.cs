using Microsoft.Extensions.Logging;
using ShapeUp.Features.Authorization.Shared.Abstractions;
using ShapeUp.Features.Notifications.Shared.Abstractions;
using ShapeUp.Features.Nutrition.Infrastructure.Policies;
using ShapeUp.Features.Relationships.Shared.Abstractions;

namespace ShapeUp.Features.Nutrition.Fasting.Shared;

/// <summary>
/// Best effort: the client's action is already saved, so a notice failure is logged and never fails the request.
/// </summary>
public sealed class FastingProfessionalNotifier(
    IInAppNoticeSender noticeSender,
    IProfessionalClientRelationshipRepository relationshipRepository,
    IUserRepository userRepository,
    ILogger<FastingProfessionalNotifier> logger) : IFastingProfessionalNotifier
{
    public Task NotifyRecommendationDeclinedAsync(int clientUserId, int? recommendedByUserId, string? reason, CancellationToken cancellationToken) =>
        NotifyAsync(clientUserId, recommendedByUserId, "Recomendação de jejum recusada",
            name => string.IsNullOrWhiteSpace(reason)
                ? $"{name} recusou a recomendação de jejum intermitente."
                : $"{name} recusou a recomendação de jejum intermitente. Motivo: {reason}",
            cancellationToken);

    public Task NotifyEndedEarlyAsync(int clientUserId, int? recommendedByUserId, string? reason, CancellationToken cancellationToken) =>
        NotifyAsync(clientUserId, recommendedByUserId, "Jejum encerrado antes do previsto",
            name => string.IsNullOrWhiteSpace(reason)
                ? $"{name} encerrou um jejum antes do previsto."
                : $"{name} encerrou um jejum antes do previsto. Motivo: {reason}",
            cancellationToken);

    private async Task NotifyAsync(
        int clientUserId,
        int? recommendedByUserId,
        string title,
        Func<string, string> body,
        CancellationToken cancellationToken)
    {
        try
        {
            var recipients = new HashSet<int>();
            if (recommendedByUserId is { } recommender)
                recipients.Add(recommender);

            var nutritionists = await relationshipRepository.ListActiveByClientAsync(
                clientUserId, NutritionAccessPolicy.RelationshipType, cancellationToken);
            foreach (var relationship in nutritionists)
                recipients.Add(relationship.ProfessionalUserId);

            recipients.Remove(clientUserId);
            if (recipients.Count == 0)
                return;

            var client = await userRepository.GetByIdAsync(clientUserId, cancellationToken);
            var text = body(string.IsNullOrWhiteSpace(client?.DisplayName) ? "Um cliente" : client!.DisplayName!);
            foreach (var recipient in recipients)
                await noticeSender.SendAsync(recipient, title, text, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Could not notify professionals about fasting activity of user {UserId}", clientUserId);
        }
    }
}
