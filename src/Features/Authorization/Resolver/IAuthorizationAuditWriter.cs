namespace ShapeUp.Features.Authorization.Resolver;

public interface IAuthorizationAuditWriter
{
    /// <summary>
    /// Records one capability decision into the existing AuditLogs store (AUTHZ-06).
    /// </summary>
    Task RecordAsync(
        int userId,
        string capability,
        bool allowed,
        string? reason,
        AuthorizationContext context,
        CancellationToken cancellationToken);
}
