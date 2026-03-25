# AuditLogs Domain - Architecture & Implementation

## Domain Scope

The `AuditLogs` domain captures API request/response traces and provides secure read access for observability.

Core responsibilities:
- Persist audit trail records for incoming HTTP requests.
- Capture user context, endpoint metadata, payload snapshots, status, and latency.
- Expose read endpoint with keyset pagination.
- Allow filtering by endpoint, method, and user email.
- Enforce access with scope `audit:logs:read`.

## Domain Structure

```text
Features/AuditLogs/
├── Shared/
│   ├── Entities/
│   │   └── AuditLogEntry.cs
│   ├── Abstractions/
│   │   └── IAuditLogRepository.cs
│   └── Data/
│       └── AuditLogsDbContext.cs
├── Infrastructure/
│   ├── Auditing/
│   │   └── AuditLoggingMiddleware.cs
│   └── Repositories/
│       └── AuditLogRepository.cs
├── GetAuditLogs/
│   ├── GetAuditLogsRequest.cs
│   ├── GetAuditLogsResponse.cs
│   ├── GetAuditLogsHandler.cs
│   └── GetAuditLogsController.cs
├── README.md
└── ARCHITECTURE.md
```

## Database Structure

### Entity
- `AuditLogEntry`: immutable-like audit row with request metadata and execution result.

### Relational Schema

```text
AuditLogEntries
├── Id (PK, bigint)
├── OccurredAtUtc (datetime)
├── UserEmail (nvarchar(320), nullable)
├── HttpMethod (nvarchar(10), required)
├── Endpoint (nvarchar(512), required)
├── QueryParametersJson (nvarchar(4000), nullable)
├── RequestBodyJson (nvarchar(4000), nullable)
├── StatusCode (int)
├── DurationMs (bigint)
├── TraceId (nvarchar(128), nullable)
├── IpAddress (nvarchar(64), nullable)
└── UserAgent (nvarchar(512), nullable)

Indexes
├── IX_AuditLogEntries_OccurredAtUtc
├── IX_AuditLogEntries_UserEmail
└── IX_AuditLogEntries_Endpoint
```

## Endpoints

### Audit Logs
- `GET /api/audit-logs`
  - Requires scope: `audit:logs:read`
  - Query params:
    - `cursor` (opaque base64 keyset cursor)
    - `pageSize` (normalized via `KeysetPageRequest`)
    - `endpoint` (optional exact match)
    - `method` (optional exact match)
    - `userEmail` (optional exact match)
  - Response shape:
    - `items`: array of `AuditLogDto`
    - `nextCursor`: `null` or base64 cursor for next page

## End-to-End Flow

1. Request enters pipeline through `AuditLoggingMiddleware`.
2. Middleware starts stopwatch and captures request metadata.
3. For write methods (`POST`, `PUT`, `PATCH`, `DELETE`), middleware buffers and captures request body.
4. Request continues to next middleware/controllers.
5. After response, middleware composes `AuditLogEntry` and persists via `IAuditLogRepository`.
6. Read access goes through `GetAuditLogsController` -> `GetAuditLogsHandler`.
7. Handler decodes cursor (`KeysetCursorCodec`) and validates request using Result Pattern.
8. Repository applies keyset (`Id < lastSeenId`) + filters at DB level, ordered by `Id DESC`.
9. Handler maps to DTO and returns `KeysetPageResponse<AuditLogDto>` with `nextCursor`.

## Key Behavior and Rules

- Controlled errors (for example invalid cursor) are returned as `Result.Failure(CommonErrors.Validation(...))`.
- Pagination is keyset-based only (no offset pagination).
- Large payload fields are truncated to max length (`4000`) before persistence.
- Middleware failures to persist logs do not break API response path (warning is logged).
- Middleware reads user context from `HttpContext.Items["User"]` when available.

## Dependency Injection and Pipeline

```text
DependencyInjectionExtensions
├── AddDbContext<AuditLogsDbContext>()
├── AddScoped<IAuditLogRepository, AuditLogRepository>()
├── AddScoped<GetAuditLogsHandler>()
└── AddScoped<AuditLoggingMiddleware>()

ApplicationBuilderExtensions.UseProjectPipeline()
├── UseHttpsRedirection()
├── UseMiddleware<AuditLoggingMiddleware>()
├── UseMiddleware<AuthorizationMiddleware>()
├── UseAuthorization()
└── MapControllers()
```

## Security and Operational Notes

- Read endpoint is protected by `RequireScopesAttribute` with `audit:logs:read`.
- Audit capture attempts to include identity context (`UserEmail`) after authorization context is available.
- Storage pressure is controlled by truncating potentially large textual fields.
- Optional filters support focused investigations without full-table scans when indexes are applicable.

## Single Source of Truth

This file is the canonical reference for AuditLogs domain architecture + implementation details.

## ASCII Diagram

```text
┌──────────────────────────────────────────────────────────────────┐
│                          API REQUEST                             │
│     Method, Path, Query, Body, Headers, TraceId, Remote IP      │
└───────────────────────────────┬──────────────────────────────────┘
                                │
                                ▼
┌──────────────────────────────────────────────────────────────────┐
│                    AuditLoggingMiddleware                        │
│ 1) Start timer                                                   │
│ 2) Capture query/body (write methods)                            │
│ 3) Call next middleware                                          │
│ 4) Persist AuditLogEntry in finally                              │
└───────────────────────────────┬──────────────────────────────────┘
                                │
                                ▼
┌──────────────────────────────────────────────────────────────────┐
│               Authorization + Controller/Handler                 │
│   GetAuditLogsController -> GetAuditLogsHandler                 │
│   RequireScopes("audit:logs:read")                              │
└───────────────────────────────┬──────────────────────────────────┘
                                │
                                ▼
┌──────────────────────────────────────────────────────────────────┐
│                    IAuditLogRepository                           │
│   AddAsync(entry) / GetPageAsync(lastSeenId, filters, pageSize) │
└───────────────────────────────┬──────────────────────────────────┘
                                │
                                ▼
┌──────────────────────────────────────────────────────────────────┐
│                    AuditLogsDbContext + SQL Server              │
│                         Table: AuditLogEntries                  │
└──────────────────────────────────────────────────────────────────┘
```
