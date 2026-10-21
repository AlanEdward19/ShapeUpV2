# Platform Admin - Architecture

## Domain Scope

Read-only operational counts for ShapeUp staff. The domain owns no tables. It composes counts that
already live in Authorization (users), Credentials (review queue) and Safety (open reports).

## Endpoints

- `GET /api/admin/overview` — `capability:platform.metrics.read` (platform admin).
  Response: `{ usersTotal, usersActive, credentialsUnderReview, openSafetyReports }`.
  Counts run in the database (`CountAsync`), never over an in-memory page.

## End-to-end flow

1. The controller requires a platform-admin capability.
2. `GetAdminOverviewHandler` asks each repository for a count.
3. The handler returns `Result.Success` with the four integers.

## ASCII Diagram

```text
┌──────────────────────────────┐
│ GET /api/admin/overview      │
│ platform.metrics.read        │
└──────────────┬───────────────┘
               │
               ▼
┌──────────────────────────────┐
│ GetAdminOverviewHandler      │
└──┬─────────┬─────────┬───────┘
   │         │         │
   ▼         ▼         ▼
 Users    Credentials  Safety
 Count    Count status Count Open
```
