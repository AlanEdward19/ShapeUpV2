# PlatformFeatureFlags Domain - Architecture & Implementation

## Domain Scope

The `PlatformFeatureFlags` domain provides a minimal, platform-admin-managed toggle store for
runtime feature switches that other domains read without redeploying.

Core responsibilities:
- Persist boolean feature flags in SQL Server (`PlatformFeatureFlag` table).
- Expose read/write HTTP endpoints for platform administrators.
- Offer `IFeatureFlagReader` with fail-open semantics (missing key → enabled).
- Seed `notifications.email-enabled = true` on first migration.

Current consumers:
- `ResendEmailNotificationSender` (`Features/Notifications`) checks
  `notifications.email-enabled` before dispatching email.

## Domain Structure

```text
Features/PlatformFeatureFlags/
├── Shared/
│   ├── Abstractions/
│   │   └── IFeatureFlagReader.cs
│   └── Entities/
│       └── PlatformFeatureFlag.cs
├── Infrastructure/
│   └── Data/
│       ├── PlatformFeatureFlagsDbContext.cs
│       ├── FeatureFlagReader.cs
│       └── Migrations/
├── GetFeatureFlags/
├── SetFeatureFlag/
├── PlatformFeatureFlagsController.cs
├── PlatformFeatureFlagsModule.cs
└── ARCHITECTURE.md
```

## Database Structure

### Relational Schema (`PlatformFeatureFlagsDbContext`)

```text
PlatformFeatureFlag
├── Key (PK, string)
├── Enabled (bool)
├── UpdatedAtUtc
└── UpdatedByUserId (nullable)
```

Seed data: `notifications.email-enabled` → `Enabled = true`.

> Deliberate SQL exception to the Mongo-for-rarely-changed heuristic (AD-013): table is tiny,
> singleton-like, and benefits from transactional consistency with other platform config.

## Endpoints

- `GET /api/platform/feature-flags`
  - Requires `capability:platform.feature_flags.manage` (platform admin).
  - Returns all flags.

- `PUT /api/platform/feature-flags/{key}`
  - Requires `capability:platform.feature_flags.manage`.
  - Body: `{ "enabled": true|false }`.
  - Records `UpdatedAtUtc` and `UpdatedByUserId` from authenticated context.

## End-to-End Flow

### Admin toggle
1. Platform admin calls `PUT` with flag key and desired state.
2. `SetFeatureFlagHandler` validates and upserts `PlatformFeatureFlag`.
3. `Result` mapped to HTTP response.

### Runtime read (fail-open)
1. Consumer (e.g. email sender) calls `IFeatureFlagReader.IsEnabledAsync(key)`.
2. Reader queries `PlatformFeatureFlagsDbContext` with `AsNoTracking`.
3. If row missing → returns `true` (fail-open: feature stays on unless explicitly disabled).
4. If row exists → returns stored `Enabled` value.

### Email guard example
1. `SendEmailTemplateHandler` or `ResendEmailNotificationSender` prepares outbound email.
2. `IsEnabledAsync("notifications.email-enabled")` runs first.
3. When disabled: log suppression, return success without calling Resend.
4. When enabled: proceed with normal Resend dispatch.

## Dependency Injection View

```text
PlatformFeatureFlagsModule
├── PlatformFeatureFlagsDbContext (SQL Server, DefaultConnection)
├── IFeatureFlagReader -> FeatureFlagReader
├── GetFeatureFlagsHandler
└── SetFeatureFlagHandler + FluentValidation

Consumers (other domains)
└── Notifications: ResendEmailNotificationSender
```

## Operational Notes

- Fail-open default avoids accidental global outage when a flag row is missing.
- Only platform admins can list or mutate flags via HTTP; domain code reads via interface only.
- New flags are added by migration seed or first `PUT`; no compile-time registry required.

## Single Source of Truth

Canonical reference for PlatformFeatureFlags architecture. Introduced by feature `nutrition`
(T6). See `.specs/features/nutrition/design.md` for rationale.

## ASCII Diagram

```text
┌─────────────────────────────────────────────────────────────────┐
│              PLATFORM ADMIN (Backoffice / API client)             │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           │ GET / PUT feature-flags
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│                    ASP.NET CORE API                             │
├─────────────────────────────────────────────────────────────────┤
│ AuthorizationMiddleware → UserContext                             │
│ [Authorize(Policy="capability:platform.feature_flags.manage")]  │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│ PlatformFeatureFlagsController                                  │
│  ├─ GetAll (list flags)                                         │
│  └─ Set (upsert by key)                                         │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│ Handlers + FluentValidation + Result pattern                    │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│ PlatformFeatureFlagsDbContext → SQL Server                      │
│ Table: PlatformFeatureFlag                                      │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           │ IFeatureFlagReader.IsEnabledAsync
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│ Other domains (read-only at runtime)                            │
│  └─ Notifications/ResendEmailNotificationSender                 │
│       notifications.email-enabled → suppress or send            │
└─────────────────────────────────────────────────────────────────┘
```
