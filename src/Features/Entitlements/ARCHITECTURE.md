# Entitlements Domain - Architecture & Implementation

## Domain Scope

The `Entitlements` domain answers "what capabilities does this user's platform tier grant?" as
one of the five capability sources consumed by the native authorization pipeline (RFC-001,
native-authorization-model, AD-002/AD-006). Like `Memberships`, it is a pure **adapter** -- it
owns no tables of its own and no `DbContext`. It reads `GymManagement`'s existing
`UserPlatformRole`/`PlatformTier` data.

Core responsibilities:
- Resolve a user's best (highest-price, active) `PlatformTier` across all their active
  `UserPlatformRole` rows.
- Map that tier to a domain-neutral `Entitlement(UserId, TierName, GrantedCapabilities)` for
  `CapabilityResolver`.

## Domain Structure

```text
Features/Entitlements/
├── Shared/
│   └── Abstractions/
│       ├── IEntitlementRepository.cs
│       └── Entitlement.cs
├── Infrastructure/
│   └── EntitlementAdapter.cs
├── EntitlementsModule.cs
└── ARCHITECTURE.md
```

No `Shared/Entities/`, no `Shared/Data/`, no migrations -- there is nothing to persist here.

## Capability Source Contract

`IEntitlementRepository.GetEntitlementAsync(userId, ct)`:
1. Loads all `UserPlatformRole` rows for the user (via `IUserPlatformRoleRepository`).
2. For each active role with a non-null `PlatformTierId`, loads the `PlatformTier` (via
   `IPlatformTierRepository`) and keeps the highest-`Price` active one (`bestTier`).
3. If no active paid tier was found, returns `Entitlement(userId, "Free", FreeTierCapabilities)`
   (currently an empty set).
4. Otherwise returns `Entitlement(userId, bestTier.Name, PaidTierCapabilities)` (currently
   `{ "advancedMetrics" }`).

`CapabilityResolver` calls this when `AuthorizationContext.RequiredEntitlementCapability` is set;
the capability is allowed if `entitlement.GrantedCapabilities.Contains(requiredCapability)`.

> **Known deviation (tracked, not a bug):** `PlatformTier` today only models gym business limits
> (`Name`, `Price`, `MaxClients`, `MaxTrainers`) -- it has no "granted capabilities" column. The
> tier-to-capabilities mapping is a hardcoded constant in `EntitlementAdapter` until Phase 5
> (Monetização, per `ROADMAP.md`) introduces a real capabilities model on `PlatformTier`. Marked
> `SPEC_DEVIATION` in the source.

## Dependency Injection

```text
EntitlementsModule
└── AddScoped<IEntitlementRepository, EntitlementAdapter>()
    (depends on GymManagement's IUserPlatformRoleRepository, IPlatformTierRepository --
     no DbContext of its own)
```

## Single Source of Truth

This file is the canonical reference for Entitlements domain architecture. See
`Features/Authorization/ARCHITECTURE.md` for how this fits into the overall capability model,
`Features/GymManagement/ARCHITECTURE.md` for the underlying `UserPlatformRole`/`PlatformTier`
data, and `docs/rfcs/rfc-001-authorization-model.md` for why this is an adapter rather than a new
table (AD-002).

## ASCII Diagram

```text
┌─────────────────────────────────────────────────────────────────┐
│ CapabilityResolver                                                │
│ (AuthorizationContext.RequiredEntitlementCapability is set)       │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│ IEntitlementRepository.GetEntitlementAsync(userId)                │
│ = EntitlementAdapter                                              │
└──────────────────────────┬──────────────────────────────────────┘
                           │
              ┌────────────┴─────────────┐
              ▼                          ▼
   ┌────────────────────────┐  ┌───────────────────────┐
   │ IUserPlatformRoleRepository│  │ IPlatformTierRepository│
   │ all active roles for user │  │ resolve tier, keep best│
   └────────────────────────┘  └───────────────────────┘
              │                          │
              └────────────┬─────────────┘
                           ▼
              no active paid tier ──► Entitlement("Free", {})
              best paid tier found ─► Entitlement(tier.Name, PaidTierCapabilities)
                           │
                           ▼
              GrantedCapabilities.Contains(required)? ──► Allow / deny
└─────────────────────────────────────────────────────────────────┘
```
