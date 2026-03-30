namespace ShapeUp.Features.AuditLogs.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Shared.Abstractions;
using Shared.Entities;
using Shared.Data;

public class AuditLogRepository(AuditLogsDbContext context) : IAuditLogRepository
{
    public async Task AddAsync(AuditLogEntry entry, CancellationToken cancellationToken)
    {
        await context.Set<AuditLogEntry>().AddAsync(entry, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditLogEntry>> GetPageAsync(
        long? lastSeenId,
        int pageSize,
        string? endpoint,
        string? method,
        string? userEmail,
        CancellationToken cancellationToken)
    {
        var query = context.Set<AuditLogEntry>().AsNoTracking();

        if (lastSeenId.HasValue)
            query = query.Where(x => x.Id < lastSeenId.Value);

        if (!string.IsNullOrWhiteSpace(endpoint))
            query = query.Where(x => x.Endpoint == endpoint);

        if (!string.IsNullOrWhiteSpace(method))
            query = query.Where(x => x.HttpMethod == method);

        if (!string.IsNullOrWhiteSpace(userEmail))
            query = query.Where(x => x.UserEmail == userEmail);

        return await query
            .OrderByDescending(x => x.Id)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }
}


