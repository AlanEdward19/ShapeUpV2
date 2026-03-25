namespace ShapeUp.Features.AuditLogs.Shared.Entities;

public class AuditLogEntry
{
    public long Id { get; set; }

    public DateTime OccurredAtUtc { get; set; }

    public string? UserEmail { get; set; }

    public required string HttpMethod { get; set; }

    public required string Endpoint { get; set; }

    public string? QueryParametersJson { get; set; }

    public string? RequestBodyJson { get; set; }

    public int StatusCode { get; set; }

    public long DurationMs { get; set; }

    public string? TraceId { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }
}

