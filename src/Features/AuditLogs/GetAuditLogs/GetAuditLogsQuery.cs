namespace ShapeUp.Features.AuditLogs.GetAuditLogs;

public record GetAuditLogsQuery(
    string? Cursor,
    int? PageSize,
    string? Endpoint,
    string? Method,
    string? UserEmail);