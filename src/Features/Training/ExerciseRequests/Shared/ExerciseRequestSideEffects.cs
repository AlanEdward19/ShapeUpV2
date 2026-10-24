using System.Text.Json;
using Microsoft.Extensions.Logging;
using ShapeUp.Features.AuditLogs.Shared.Abstractions;
using ShapeUp.Features.AuditLogs.Shared.Entities;
using ShapeUp.Features.Authorization.Shared.Abstractions;
using ShapeUp.Features.Notifications.Shared.Abstractions;

namespace ShapeUp.Features.Training.ExerciseRequests.Shared;

/// <summary>Audit and notice side effects of the request lifecycle.</summary>
public interface IExerciseRequestSideEffects
{
    Task AuditAsync(string action, int actorUserId, int requestId, int? exerciseId, string? detail, CancellationToken cancellationToken);

    Task NotifyApprovedAsync(int requesterUserId, string exerciseName, CancellationToken cancellationToken);

    Task NotifyRejectedAsync(int requesterUserId, string requestName, string reason, CancellationToken cancellationToken);
}

/// <summary>
/// Best effort, like the HTTP audit middleware: the review is already committed, so a failure here is logged and never
/// fails the request.
/// </summary>
public sealed class ExerciseRequestSideEffects(
    IAuditLogRepository auditLogRepository,
    IUserRepository userRepository,
    IInAppNoticeSender noticeSender,
    ILogger<ExerciseRequestSideEffects> logger) : IExerciseRequestSideEffects
{
    public async Task AuditAsync(
        string action, int actorUserId, int requestId, int? exerciseId, string? detail, CancellationToken cancellationToken)
    {
        try
        {
            var actor = await userRepository.GetByIdAsync(actorUserId, cancellationToken);
            var occurredAt = DateTime.UtcNow;
            await auditLogRepository.AddAsync(new AuditLogEntry
            {
                OccurredAtUtc = occurredAt,
                UserEmail = actor?.Email,
                HttpMethod = "AUDIT",
                Endpoint = $"training.exercise-request.{action}",
                RequestBodyJson = JsonSerializer.Serialize(new
                {
                    actorUserId, requestId, exerciseId, detail, occurredAtUtc = occurredAt
                }),
                StatusCode = 200
            }, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Could not write the audit entry for exercise request {RequestId}", requestId);
        }
    }

    public Task NotifyApprovedAsync(int requesterUserId, string exerciseName, CancellationToken cancellationToken) =>
        SendAsync(requesterUserId, "Exercício aprovado",
            $"Seu pedido foi aprovado e o exercício \"{exerciseName}\" já está na biblioteca.", cancellationToken);

    public Task NotifyRejectedAsync(int requesterUserId, string requestName, string reason, CancellationToken cancellationToken) =>
        SendAsync(requesterUserId, "Pedido de exercício recusado",
            $"Seu pedido de \"{requestName}\" não foi aceito. Motivo: {reason}", cancellationToken);

    private async Task SendAsync(int userId, string title, string body, CancellationToken cancellationToken)
    {
        try
        {
            await noticeSender.SendAsync(userId, title, body, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Could not notify user {UserId} about an exercise request", userId);
        }
    }
}
