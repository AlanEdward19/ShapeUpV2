# Relationships Domain - Architecture & Implementation

## Domain Scope

The `Relationships` domain tracks professional-client relationships (e.g. a trainer's
independent, non-gym client) as one of the five capability sources consumed by the native
authorization pipeline (RFC-001, native-authorization-model, AD-002/AD-005/AD-006). It owns a
genuinely new table -- unlike `Memberships`/`Entitlements`, there is no existing `GymManagement`
data this adapts.

Core responsibilities:
- Persist `ProfessionalClientRelationship` records (professional, client, relationship type,
  status, start/end).
- Answer "does this professional currently have an active relationship of this type with this
  client?" for `CapabilityResolver` and for `Training`'s `ITrainingAccessPolicy` (AD-005).
- Enforce at most one Active relationship per `(professional, client, type)` tuple.

## Domain Structure

```text
Features/Relationships/
├── Shared/
│   ├── Entities/
│   │   ├── ProfessionalClientRelationship.cs
│   │   └── RelationshipStatus.cs
│   ├── Abstractions/
│   │   └── IProfessionalClientRelationshipRepository.cs
│   └── Data/
│       ├── RelationshipsDbContext.cs
│       └── Migrations/
├── Infrastructure/
│   └── Repositories/
│       └── ProfessionalClientRelationshipRepository.cs
├── RelationshipsModule.cs
└── ARCHITECTURE.md
```

## Database Structure

### Entity: `ProfessionalClientRelationship`
- `Id` (PK)
- `ProfessionalUserId`, `ClientUserId`.
- `RelationshipType` -- free-form identifier (e.g. `"Training"`); matched against
  `AuthorizationContext.RelationshipType`.
- `Status` (`RelationshipStatus`: `Active` | `Ended`).
- `StartedAt`, `EndedAt` (nullable).

A unique filtered database index enforces at most one `Active` row per
`(ProfessionalUserId, ClientUserId, RelationshipType)` -- `CreateAsync` relies on this index
(fails with a `Conflict` `Result` on violation) rather than an application-level
check-then-insert, to stay race-safe.

## Capability Source Contract

- `GetActiveAsync(professionalUserId, clientUserId, relationshipType, ct)` returns the `Active`
  relationship for the pair/type, or `null` if none exists (including one that has been
  `Ended`). `CapabilityResolver` calls this when both `AuthorizationContext.TargetUserId` and
  `RelationshipType` are set.
- `CreateAsync(relationship, ct)` creates an `Active` relationship, relying on the unique index
  above for conflict detection.

### Consumers

- `CapabilityResolver` (`Features/Authorization/Resolver/CapabilityResolver.cs`) -- generic
  capability source.
- `ITrainingAccessPolicy` (`Features/Training/Infrastructure/Policies/TrainingAccessPolicy.cs`)
  -- calls `GetActiveAsync(actor, target, "Training", ct)` directly, since Training's routes
  can't be authorized via `[Authorize(Policy=...)]` (AD-005).

## Dependency Injection

```text
RelationshipsModule
├── AddDbContext<RelationshipsDbContext>()
└── AddScoped<IProfessionalClientRelationshipRepository, ProfessionalClientRelationshipRepository>()
```

## Single Source of Truth

This file is the canonical reference for Relationships domain architecture. See
`Features/Authorization/ARCHITECTURE.md` for how this fits into the overall capability model,
`Features/Training/ARCHITECTURE.md` for the AD-005 exception, and
`docs/rfcs/rfc-001-authorization-model.md` for the decision record.

## ASCII Diagram

```text
┌─────────────────────────────────────────────────────────────────┐
│ CapabilityResolver               │   ITrainingAccessPolicy      │
│ (TargetUserId + RelationshipType)│   (self-access failed first) │
└──────────────────┬────────────────────────────┬─────────────────┘
                   │                            │
                   ▼                            ▼
┌─────────────────────────────────────────────────────────────────┐
│ IProfessionalClientRelationshipRepository.GetActiveAsync(        │
│   professionalUserId, clientUserId, relationshipType)            │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│ RelationshipsDbContext -> ProfessionalClientRelationships table  │
│ Match: ProfessionalUserId + ClientUserId + RelationshipType      │
│        + Status=Active                                          │
└──────────────────────────┬──────────────────────────────────────┘
                           │
              found ──────► Allow
              not found ──► fall through / deny
└─────────────────────────────────────────────────────────────────┘
```
