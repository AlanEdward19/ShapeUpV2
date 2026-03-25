namespace ShapeUp.Features.AuditLogs.Infrastructure.Auditing;

using System.Diagnostics;
using System.Text.Json;
using Shared.Abstractions;
using ShapeUp.Features.Authorization.Infrastructure.Authorization;

public class AuditLoggingMiddleware(ILogger<AuditLoggingMiddleware> logger) : IMiddleware
{
    private const int MaxPayloadLength = 4000;

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var cancellationToken = context.RequestAborted;
        var startedAt = DateTime.UtcNow;
        var sw = Stopwatch.StartNew();

        string? requestBody = null;
        if (ShouldCaptureBody(context.Request.Method))
            requestBody = await ReadRequestBodyAsync(context.Request);

        var queryJson = SerializeQuery(context.Request.Query);

        try
        {
            await next(context);
        }
        finally
        {
            sw.Stop();

            try
            {
                var repository = context.RequestServices.GetRequiredService<IAuditLogRepository>();
                var userContext = context.Items.TryGetValue("User", out var userObj) ? userObj as UserContext : null;

                var entry = new Shared.Entities.AuditLogEntry
                {
                    OccurredAtUtc = startedAt,
                    UserEmail = Truncate(userContext?.Email, 320),
                    HttpMethod = Truncate(context.Request.Method, 10) ?? "UNKNOWN",
                    Endpoint = Truncate(context.Request.Path.Value ?? string.Empty, 512) ?? string.Empty,
                    QueryParametersJson = Truncate(queryJson, MaxPayloadLength),
                    RequestBodyJson = Truncate(requestBody, MaxPayloadLength),
                    StatusCode = context.Response.StatusCode,
                    DurationMs = sw.ElapsedMilliseconds,
                    TraceId = Truncate(context.TraceIdentifier, 128),
                    IpAddress = Truncate(context.Connection.RemoteIpAddress?.ToString(), 64),
                    UserAgent = Truncate(context.Request.Headers.UserAgent.ToString(), 512)
                };

                await repository.AddAsync(entry, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to persist audit log entry.");
            }
        }
    }

    private static bool ShouldCaptureBody(string method) =>
        method.Equals("POST", StringComparison.OrdinalIgnoreCase)
        || method.Equals("PUT", StringComparison.OrdinalIgnoreCase)
        || method.Equals("PATCH", StringComparison.OrdinalIgnoreCase)
        || method.Equals("DELETE", StringComparison.OrdinalIgnoreCase);

    private static async Task<string?> ReadRequestBodyAsync(HttpRequest request)
    {
        if (request.ContentLength is null or <= 0)
            return null;

        request.EnableBuffering();

        using var reader = new StreamReader(request.Body, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        request.Body.Position = 0;

        return body;
    }

    private static string? SerializeQuery(IQueryCollection query)
    {
        if (query.Count == 0)
            return null;

        var dictionary = query.ToDictionary(x => x.Key, x => x.Value.ToString());
        return JsonSerializer.Serialize(dictionary);
    }

    private static string? Truncate(string? value, int max)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        return value.Length <= max ? value : value[..max];
    }
}

