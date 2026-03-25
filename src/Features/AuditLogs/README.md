# Audit Logs Domain

`AuditLogs` captures request/response traces and exposes a protected read endpoint with keyset pagination.

## Quick Links
- Architecture + implementation details: `Features/AuditLogs/ARCHITECTURE.md`
- Endpoint contract: `GET /api/audit-logs`

## What Is Captured
- `UserEmail`
- `OccurredAtUtc`
- `HttpMethod`
- `Endpoint`
- `QueryParametersJson`
- `RequestBodyJson` (truncated)
- `StatusCode`
- `DurationMs`
- `TraceId`, `IpAddress`, `UserAgent`

## Access and Pagination
- Required scope: `audit:logs:read`
- Keyset cursor: opaque base64 encoded `Id`
- Supported filters: `endpoint`, `method`, `userEmail`

Example:
`GET /api/audit-logs?cursor={base64}&pageSize=20&endpoint=/api/groups&method=POST&userEmail=user@site.com`

Response shape:
- `items`: array of audit rows
- `nextCursor`: cursor for next page, `null` when finished

## Notes
- Pagination is applied at repository/query level.
- Request payload fields are truncated to reduce storage pressure.
- Middleware persistence errors are logged and do not break the request pipeline.
