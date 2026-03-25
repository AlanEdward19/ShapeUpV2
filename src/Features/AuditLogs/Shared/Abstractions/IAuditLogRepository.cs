namespace ShapeUp.Features.AuditLogs.Shared.Abstractions;

using Entities;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLogEntry entry, CancellationToken cancellationToken);

    Task<IReadOnlyList<AuditLogEntry>> GetPageAsync(
        long? lastSeenId,
        int pageSize,
        string? endpoint,
        string? method,
        string? userEmail,
        CancellationToken cancellationToken);
}

