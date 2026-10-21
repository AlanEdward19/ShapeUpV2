using Microsoft.EntityFrameworkCore;

namespace ShapeUp.Features.Safety.Shared;

public interface ISafetyReportCounts
{
    Task<int> CountByStatusAsync(string status, CancellationToken cancellationToken);
}

public class SafetyReportCounts(SafetyDbContext db) : ISafetyReportCounts
{
    public Task<int> CountByStatusAsync(string status, CancellationToken cancellationToken) =>
        db.ContentReports.CountAsync(r => r.Status == status, cancellationToken);
}
