using ShapeUp.Features.Authorization.ProfessionalCapabilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ShapeUp.Features.Authorization.Shared.Abstractions;
using ShapeUp.Features.Notifications.Shared.Abstractions;
using ShapeUp.Features.Notifications.Shared.Helpers;
using ShapeUp.Features.Notifications.Shared.Models;
using ShapeUp.Features.Nutrition.Clients.Shared;
using ShapeUp.Features.Nutrition.Infrastructure.Policies;
using ShapeUp.Features.Relationships.Shared.Abstractions;
using ShapeUp.Features.Relationships.Shared.Entities;
using ShapeUp.Shared.Invites;
using ShapeUp.Shared.Results;
using System.Security.Cryptography;
using System.Text;

namespace ShapeUp.Features.Nutrition.Clients.InviteClient;

/// <summary>
/// The nutritionist generates a single-use token and hands it to the client (link, QR, message).
/// Nothing is shared until the client accepts it.
/// </summary>
public class InviteNutritionClientHandler(
    IProfessionalCapabilityService capabilityService,
    IProfessionalClientInviteRepository inviteRepository,
    TimeProvider? timeProvider = null,
    IEmailNotificationSender? emailSender = null,
    IUserRepository? userRepository = null,
    IOptions<NutritionInviteLinkOptions>? linkOptions = null,
    ILogger<InviteNutritionClientHandler>? logger = null)
{
    /// <summary>Pending (not expired) invites a nutritionist may hold at once; same rule as the trainer's (<see cref="InviteRules"/>).</summary>
    public const int MaxPendingInvites = InviteRules.MaxWaitingInvites;

    public async Task<Result<InviteNutritionClientResponse>> HandleAsync(
        int nutritionistUserId, InviteNutritionClientCommand? command, CancellationToken cancellationToken)
    {
        if (command?.ExpiresInHours is { } hours && !InviteRules.IsValidLifetime(hours))
            return Result<InviteNutritionClientResponse>.Failure(CommonErrors.Validation(
                $"ExpiresInHours must be between {InviteRules.MinLifetimeHours} and {InviteRules.MaxLifetimeHours}."));

        var email = command?.ClientEmail?.Trim().ToLowerInvariant();
        if (!string.IsNullOrEmpty(email) && !IsValidEmail(email))
            return Result<InviteNutritionClientResponse>.Failure(CommonErrors.Validation("ClientEmail is not a valid e-mail address."));
        if (string.IsNullOrEmpty(email))
            email = null;

        var capabilities = await capabilityService.GetAsync(nutritionistUserId, cancellationToken);
        if (!capabilities.Nutrition)
            return Result<InviteNutritionClientResponse>.Failure(CommonErrors.Forbidden("Nutrition capability is required."));

        var nowUtc = (timeProvider ?? TimeProvider.System).GetUtcNow().UtcDateTime;
        // Best effort under concurrency (check then insert): a burst may overshoot the limit by a few invites.
        // A new invite to the same e-mail replaces the one still waiting, so re-sending never hits the cap.
        var superseded = email is null
            ? null
            : await inviteRepository.GetPendingByEmailAsync(nutritionistUserId, NutritionAccessPolicy.RelationshipType, email, nowUtc, cancellationToken);
        var pending = await inviteRepository.CountPendingAsync(nutritionistUserId, NutritionAccessPolicy.RelationshipType, nowUtc, cancellationToken);
        if (pending - (superseded is null ? 0 : 1) >= MaxPendingInvites)
            return Result<InviteNutritionClientResponse>.Failure(CommonErrors.Conflict(InviteRules.LimitMessage));

        var token = GenerateToken();
        var invite = new ProfessionalClientInvite
        {
            ProfessionalUserId = nutritionistUserId,
            RelationshipType = NutritionAccessPolicy.RelationshipType,
            TokenHash = ComputeHash(token),
            CreatedAtUtc = nowUtc,
            ExpiresAtUtc = InviteRules.ExpiresAt(nowUtc, command?.ExpiresInHours),
            InviteeEmail = email
        };
        await inviteRepository.AddAsync(invite, cancellationToken);

        if (superseded is not null)
            await inviteRepository.TryRevokeAsync(superseded.Id, nutritionistUserId, cancellationToken);

        var options = linkOptions?.Value ?? new NutritionInviteLinkOptions();
        var url = options.Build(token);
        var (sent, error) = email is null
            ? (false, null)
            : await SendEmailAsync(nutritionistUserId, email, url, invite.ExpiresAtUtc, options, cancellationToken);

        return Result<InviteNutritionClientResponse>.Success(
            new InviteNutritionClientResponse(token, invite.ExpiresAtUtc, invite.Id, url, email, sent, error));
    }

    /// <summary>The invite is already saved; a provider failure is reported in the response so the nutritionist can share the link instead.</summary>
    private async Task<(bool Sent, string? Error)> SendEmailAsync(
        int nutritionistUserId, string email, string url, DateTime expiresAtUtc, NutritionInviteLinkOptions options, CancellationToken cancellationToken)
    {
        if (emailSender is null)
            return (false, "E-mail sending is not configured.");

        try
        {
            var nutritionist = userRepository is null ? null : await userRepository.GetByIdAsync(nutritionistUserId, cancellationToken);
            var name = string.IsNullOrWhiteSpace(nutritionist?.DisplayName) ? "Seu nutricionista" : nutritionist!.DisplayName!;
            var days = Math.Max(1, (int)Math.Ceiling((expiresAtUtc - DateTime.UtcNow).TotalDays));
            var html = WarmOxideEmailTemplate.Render(
                "Convite pessoal",
                $"{name} convidou você",
                [
                    $"{name} quer acompanhar a sua nutrição no ShapeUp: metas, plano alimentar e evolução.",
                    "Toque no botão para abrir o app com o convite já preenchido. Se você ainda não tem conta, crie uma em um minuto.",
                    $"O convite vale por {(days == 1 ? "1 dia" : $"{days} dias")} e só pode ser usado uma vez. Você escolhe se aceita: nada é compartilhado antes disso."
                ],
                ctaText: "Aceitar convite",
                ctaUrl: url,
                footer: "Você recebeu esta mensagem porque um nutricionista informou o seu e-mail no ShapeUp.");

            var result = await emailSender.SendHtmlAsync(
                new SendHtmlEmailRequest(email, string.Format(options.EmailSubject, name), html), cancellationToken);
            if (result.IsSuccess)
                return (true, null);

            // The message carries the token in its link: log the provider error, never the request.
            logger?.LogWarning("Nutrition invite e-mail failed: {Error}", result.Error?.Message);
            return (false, "The e-mail could not be sent. Share the invite link instead.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Nutrition invite e-mail failed");
            return (false, "The e-mail could not be sent. Share the invite link instead.");
        }
    }

    private static bool IsValidEmail(string email) =>
        email.Length <= 256 && System.Net.Mail.MailAddress.TryCreate(email, out var parsed) && parsed.Address == email && email.Contains('.');

    internal static string GenerateToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public static string ComputeHash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
