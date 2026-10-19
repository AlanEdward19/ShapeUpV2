namespace ShapeUp.Features.Credentials.Infrastructure.Notifications;

using Microsoft.Extensions.Logging;
using Shared.Entities;
using Shared.Notifications;
using ShapeUp.Features.Authorization.Shared.Abstractions;
using ShapeUp.Features.Notifications.Shared.Abstractions;
using ShapeUp.Features.Notifications.Shared.Helpers;
using ShapeUp.Features.Notifications.Shared.Models;

/// <summary>
/// E-mails the professional on each registration step. A failure (provider down, unknown user) is logged and
/// retried a few times, then dropped: the status change that triggered the message is never undone.
/// </summary>
public class EmailCredentialNotifier(
    IUserRepository userRepository,
    IEmailNotificationSender emailSender,
    ILogger<EmailCredentialNotifier>? logger = null) : ICredentialNotifier
{
    private const string Footer = "Você recebeu esta mensagem porque enviou um registro profissional no ShapeUp.";

    private static readonly TimeSpan[] RetryDelays = [TimeSpan.FromMilliseconds(300), TimeSpan.FromSeconds(2)];

    public Task ReceivedAsync(ProfessionalCredential credential, CancellationToken cancellationToken) =>
        SendAsync(credential, "Recebemos o seu registro profissional", (name, council) => WarmOxideEmailTemplate.Render(
            "Registro profissional",
            "Registro recebido",
            [
                $"Olá, {name}. Recebemos o seu registro {council} e ele entrou em análise.",
                "Assim que houver uma decisão, avisamos você por aqui. Você também pode acompanhar o status em Configurações > Quero ser profissional."
            ],
            "Registro", council, footer: Footer), cancellationToken);

    public Task ApprovedAsync(ProfessionalCredential credential, CancellationToken cancellationToken) =>
        SendAsync(credential, "Seu registro profissional foi aprovado", (name, council) => WarmOxideEmailTemplate.Render(
            "Registro profissional",
            "Registro aprovado",
            [
                $"Olá, {name}. O seu registro {council} foi aprovado e o acesso profissional foi liberado.",
                "Abra o ShapeUp e, em Configurações, toque em Atualizar perfil para entrar no modo Profissional."
            ],
            "Registro", council, footer: Footer), cancellationToken);

    public Task RejectedAsync(ProfessionalCredential credential, CancellationToken cancellationToken) =>
        SendAsync(credential, "Seu registro profissional não foi aprovado", (name, council) => WarmOxideEmailTemplate.Render(
            "Registro profissional",
            "Registro não aprovado",
            [
                $"Olá, {name}. Não conseguimos aprovar o seu registro {council}.",
                "Se achar que houve engano, envie o registro novamente com os dados corretos."
            ],
            "Motivo", credential.RejectionReason ?? "Registro não confirmado.", footer: Footer), cancellationToken);

    public Task ChangesRequestedAsync(ProfessionalCredential credential, CancellationToken cancellationToken) =>
        SendAsync(credential, "Precisamos de um ajuste no seu registro profissional", (name, council) => WarmOxideEmailTemplate.Render(
            "Registro profissional",
            "Ajuste solicitado",
            [
                $"Olá, {name}. Para concluir a análise do seu registro {council}, precisamos de uma correção.",
                "Abra Configurações > Quero ser profissional, ajuste os dados e envie de novo. Não é preciso começar do zero."
            ],
            "O que ajustar", credential.ChangesRequestedMessage ?? "Revise os dados do registro.", footer: Footer), cancellationToken);

    public Task AccessEndedAsync(ProfessionalCredential credential, CancellationToken cancellationToken)
    {
        var (title, what) = credential.Status switch
        {
            CredentialStatus.Suspended => ("Acesso profissional suspenso", "foi suspenso"),
            CredentialStatus.Revoked => ("Acesso profissional revogado", "foi revogado"),
            _ => ("Registro profissional expirado", "expirou")
        };
        var closing = credential.Status == CredentialStatus.Expired
            ? "Para voltar a usar o modo Profissional, envie o registro novamente em Configurações > Quero ser profissional."
            : "Em caso de dúvida, responda a este e-mail ou procure o suporte do ShapeUp.";

        return SendAsync(credential, title, (name, council) => WarmOxideEmailTemplate.Render(
            "Registro profissional",
            title,
            [$"Olá, {name}. O seu registro {council} {what} e o acesso profissional foi retirado.", closing],
            credential.Status == CredentialStatus.Expired ? null : "Motivo",
            credential.Status == CredentialStatus.Expired ? null : credential.EndReason, footer: Footer), cancellationToken);
    }

    private async Task SendAsync(
        ProfessionalCredential credential, string subject, Func<string, string, string> render, CancellationToken cancellationToken)
    {
        try
        {
            var user = await userRepository.GetByIdAsync(credential.UserId, cancellationToken);
            if (user is null || string.IsNullOrWhiteSpace(user.Email))
            {
                logger?.LogWarning("Credential {CredentialId}: no e-mail address for user {UserId}; notification skipped", credential.Id, credential.UserId);
                return;
            }

            var council = $"{credential.IssuingAuthority} {credential.CredentialNumber}/{credential.IssuingRegion}";
            var html = render(string.IsNullOrWhiteSpace(user.DisplayName) ? "profissional" : user.DisplayName, council);
            var request = new SendHtmlEmailRequest(user.Email, subject, html);

            for (var attempt = 0; ; attempt++)
            {
                var result = await emailSender.SendHtmlAsync(request, cancellationToken);
                if (result.IsSuccess)
                    return;

                logger?.LogWarning("Credential {CredentialId}: e-mail '{Subject}' failed (attempt {Attempt}): {Error}",
                    credential.Id, subject, attempt + 1, result.Error?.Message);
                if (attempt >= RetryDelays.Length)
                    return;
                await Task.Delay(RetryDelays[attempt], cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The request ended; the status change already stands.
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Credential {CredentialId}: e-mail '{Subject}' could not be sent", credential.Id, subject);
        }
    }
}
