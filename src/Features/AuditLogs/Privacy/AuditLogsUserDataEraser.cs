namespace ShapeUp.Features.AuditLogs.Privacy;

using Microsoft.EntityFrameworkCore;
using Shared.Data;
using ShapeUp.Shared.Privacy;

/// <summary>Request logs are identified by e-mail; the user's rows go, since they hold query strings and bodies.</summary>
public class AuditLogsUserDataEraser(AuditLogsDbContext db) : IUserDataEraser
{
    public string Module => "AuditLogs";

    public async Task EraseAsync(int userId, string? email, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email))
            return;
        await db.AuditLogEntries.Where(e => e.UserEmail == email).ExecuteDeleteAsync(cancellationToken);
    }
}
