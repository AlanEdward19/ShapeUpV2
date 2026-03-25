namespace ShapeUp.Features.AuditLogs.GetAuditLogs;

using Shared.Abstractions;
using ShapeUp.Shared.Pagination;
using ShapeUp.Shared.Results;

public class GetAuditLogsHandler(IAuditLogRepository repository)
{
    public async Task<Result<KeysetPageResponse<AuditLogDto>>> HandleAsync(
        GetAuditLogsQuery query,
        CancellationToken cancellationToken)
    {
        long? lastSeenId = null;
        if (!string.IsNullOrWhiteSpace(query.Cursor))
        {
            if (!KeysetCursorCodec.TryDecodeLong(query.Cursor, out var decoded))
            {
                return Result<KeysetPageResponse<AuditLogDto>>.Failure(
                    CommonErrors.Validation("Invalid keyset cursor."));
            }

            lastSeenId = decoded;
        }

        var pageRequest = new KeysetPageRequest(query.Cursor, query.PageSize);
        var pageSize = pageRequest.NormalizePageSize();

        var entries = await repository.GetPageAsync(
            lastSeenId,
            pageSize,
            query.Endpoint,
            query.Method,
            query.UserEmail,
            cancellationToken);

        var items = entries
            .Select(e => new AuditLogDto(
                e.Id,
                e.OccurredAtUtc,
                e.UserEmail,
                e.HttpMethod,
                e.Endpoint,
                e.QueryParametersJson,
                e.RequestBodyJson,
                e.StatusCode,
                e.DurationMs,
                e.TraceId,
                e.IpAddress,
                e.UserAgent))
            .ToArray();

        var nextCursor = items.Length < pageSize
            ? null
            : KeysetCursorCodec.EncodeLong(items[^1].Id);

        return Result<KeysetPageResponse<AuditLogDto>>.Success(
            new KeysetPageResponse<AuditLogDto>(items, nextCursor));
    }
}

