namespace ShapeUp.Features.AuditLogs.GetAuditLogs;

public record AuditLogDto(
    long Id,
    DateTime OccurredAtUtc,
    string? UserEmail,
    string HttpMethod,
    string Endpoint,
    string? QueryParametersJson,
    string? RequestBodyJson,
    int StatusCode,
    long DurationMs,
    string? TraceId,
    string? IpAddress,
    string? UserAgent);