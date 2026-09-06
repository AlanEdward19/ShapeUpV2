namespace ShapeUp.Features.Authorization.Resolver;

using System.Text.Json;
using ShapeUp.Features.AuditLogs.Shared.Abstractions;
using ShapeUp.Features.AuditLogs.Shared.Entities;

/// <summary>
/// Records capability decisions into the existing AuditLogs store. Reuses AuditLogEntry
/// (an HTTP-request-shaped table) rather than introducing a new one: Endpoint carries the
/// capability name, HttpMethod is the fixed marker "AUTHZ", StatusCode is 200/403, and
/// RequestBodyJson carries the deny reason + context when denied.
/// </summary>
public class AuthorizationAuditWriter(IAuditLogRepository auditLogRepository) : IAuthorizationAuditWriter
{
    private const string AuthorizationDecisionMethod = "AUTHZ";

    public async Task RecordAsync(
        int userId,
        string capability,
        bool allowed,
        string? reason,
        AuthorizationContext context,
        CancellationToken cancellationToken)
    {
        var entry = new AuditLogEntry
        {
            OccurredAtUtc = DateTime.UtcNow,
            HttpMethod = AuthorizationDecisionMethod,
            Endpoint = capability,
            StatusCode = allowed ? 200 : 403,
            DurationMs = 0,
            RequestBodyJson = JsonSerializer.Serialize(new
            {
                userId,
                allowed,
                reason,
                context.GymId,
                context.TargetUserId
            })
        };

        await auditLogRepository.AddAsync(entry, cancellationToken);
    }
}
