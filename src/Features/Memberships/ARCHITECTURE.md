# Memberships Domain - Architecture & Implementation

## Domain Scope

The `Memberships` domain answers "is this user a member of this gym, and with what role?" as one
of the five capability sources consumed by the native authorization pipeline (RFC-001,
native-authorization-model, AD-002/AD-006). Unlike `Credentials`/`Relationships`, it is a pure
**adapter** -- it owns no tables of its own and no `DbContext`. It reads directly from
`GymManagement`'s existing `Gym`/`GymStaff` data.

Core responsibilities:
- Map `GymManagement.Gym.OwnerId` and `GymManagement.GymStaff` into a domain-neutral
  `OrganizationMembership(UserId, GymId, MembershipRole)` for `CapabilityResolver`.
- Translate `GymManagement`'s `GymStaffRole` enum into the authorization domain's own
  `MembershipRole` enum, so `Authorization` never depends on `GymManagement` entities directly.

## Domain Structure

```text
Features/Memberships/
├── Shared/
│   └── Abstractions/
│       ├── IOrganizationMembershipRepository.cs
│       ├── OrganizationMembership.cs
│       └── MembershipRole.cs
├── Infrastructure/
│   └── OrganizationMembershipAdapter.cs
├── MembershipsModule.cs
└── ARCHITECTURE.md
```

No `Shared/Entities/`, no `Shared/Data/`, no migrations -- there is nothing to persist here.

## Capability Source Contract

`IOrganizationMembershipRepository.GetMembershipAsync(userId, gymId, ct)`:
1. Loads the `Gym` by id (via `IGymRepository`, from `GymManagement`). Returns `null` if the gym
   doesn't exist.
2. If `gym.OwnerId == userId`, returns `OrganizationMembership(userId, gymId, MembershipRole.Owner)`
   immediately -- gym ownership always outranks a staff row.
3. Otherwise looks up `GymStaff` for `(gymId, userId)` (via `IGymStaffRepository`). Returns
   `null` if no row, or if the row exists but `IsActive` is false.
4. Maps the staff `GymStaffRole` to `MembershipRole`:

```text
GymStaffRole.Trainer      -> MembershipRole.Trainer
GymStaffRole.Receptionist -> MembershipRole.Receptionist
GymStaffRole.Manager      -> MembershipRole.Manager
GymStaffRole.Finance      -> MembershipRole.Finance
GymStaffRole.Staff        -> MembershipRole.Staff
```

`CapabilityResolver` calls this when `AuthorizationContext.GymId` is set; a non-null result
allows the capability, independent of which `MembershipRole` was returned (the resolver does not
currently branch on role -- see `Features/Authorization/ARCHITECTURE.md`'s Future Enhancements
for modeling role-specific capability gates).

## Dependency Injection

```text
MembershipsModule
└── AddScoped<IOrganizationMembershipRepository, OrganizationMembershipAdapter>()
    (depends on GymManagement's IGymRepository, IGymStaffRepository -- no DbContext of its own)
```

## Single Source of Truth

This file is the canonical reference for Memberships domain architecture. See
`Features/Authorization/ARCHITECTURE.md` for how this fits into the overall capability model,
`Features/GymManagement/ARCHITECTURE.md` for the underlying `Gym`/`GymStaff` data, and
`docs/rfcs/rfc-001-authorization-model.md` for why this is an adapter rather than a new table
(AD-002).

## ASCII Diagram

```text
┌─────────────────────────────────────────────────────────────────┐
│ CapabilityResolver (AuthorizationContext.GymId is set)           │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│ IOrganizationMembershipRepository.GetMembershipAsync(             │
│   userId, gymId)                                                  │
│ = OrganizationMembershipAdapter                                  │
└──────────────────────────┬──────────────────────────────────────┘
                           │
              ┌────────────┴─────────────┐
              ▼                          ▼
   ┌────────────────────┐     ┌───────────────────────┐
   │ IGymRepository      │     │ IGymStaffRepository    │
   │ Gym.OwnerId == user?│     │ GymStaff(gymId, user)  │
   │  -> Owner            │     │  IsActive? -> map role │
   └────────────────────┘     └───────────────────────┘
              │                          │
              └────────────┬─────────────┘
                           ▼
              found ──────► Allow (OrganizationMembership)
              not found ──► fall through / deny
└─────────────────────────────────────────────────────────────────┘
```
