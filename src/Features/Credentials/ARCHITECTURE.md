# Credentials Domain - Architecture & Implementation

## Domain Scope

The `Credentials` domain tracks professional credentials (e.g. a personal trainer's
certification) as one of the five capability sources consumed by the native authorization
pipeline (RFC-001, native-authorization-model, AD-002/AD-006). It owns a genuinely new table --
unlike `Memberships`/`Entitlements`, there is no existing `GymManagement` data this adapts.

Core responsibilities:
- Persist `ProfessionalCredential` records (profession type, issuing authority/region, country,
  credential number, verification status, expiry).
- Answer "does this user hold a currently verified, non-expired credential for this profession
  type?" for `CapabilityResolver`.

Out of scope: verification workflow UI/process, credential issuance integrations -- this domain
only stores state and answers the verified/not-verified question.

## Domain Structure

```text
Features/Credentials/
├── Shared/
│   ├── Entities/
│   │   ├── ProfessionalCredential.cs
│   │   └── CredentialStatus.cs
│   ├── Abstractions/
│   │   └── IProfessionalCredentialRepository.cs
│   ├── StateMachine/
│   │   └── CredentialStatusGuard.cs
│   └── Data/
│       ├── CredentialsDbContext.cs
│       └── Migrations/
├── Infrastructure/
│   └── Repositories/
│       └── ProfessionalCredentialRepository.cs
├── CredentialsModule.cs
└── ARCHITECTURE.md
```

## Database Structure

### Entity: `ProfessionalCredential`
- `Id` (PK)
- `UserId` -- the credential holder.
- `ProfessionType` -- free-form profession identifier (e.g. `"PersonalTrainer"`); matched
  against `AuthorizationContext.RequiredProfessionType`.
- `CredentialNumber`, `IssuingAuthority`, `IssuingRegion`, `Country`.
- `Status` (`CredentialStatus`: `Draft` | ... | `Verified` | ...) -- see `CredentialStatusGuard`
  for the allowed transitions.
- `VerifiedAt`, `ExpiresAt` (nullable).

## Capability Source Contract

`IProfessionalCredentialRepository.GetVerifiedAsync(userId, professionType, nowUtc, ct)` returns
the credential only when it is `Verified` and (`ExpiresAt is null or ExpiresAt > nowUtc`); it
returns `null` for any other status, for an expired credential, or when none exists. This is the
single method `CapabilityResolver` calls when `AuthorizationContext.RequiredProfessionType` is
set -- a non-null result allows the capability.

## Dependency Injection

```text
CredentialsModule
├── AddDbContext<CredentialsDbContext>()
└── AddScoped<IProfessionalCredentialRepository, ProfessionalCredentialRepository>()
```

## Single Source of Truth

This file is the canonical reference for Credentials domain architecture. See
`Features/Authorization/ARCHITECTURE.md` for how this fits into the overall capability model and
`docs/rfcs/rfc-001-authorization-model.md` for the decision record.

## ASCII Diagram

```text
┌─────────────────────────────────────────────────────────────────┐
│ CapabilityResolver                                               │
│ (AuthorizationContext.RequiredProfessionType is set)             │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│ IProfessionalCredentialRepository.GetVerifiedAsync(              │
│   userId, professionType, nowUtc)                                │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│ CredentialsDbContext -> ProfessionalCredentials table            │
│ Match: UserId + ProfessionType + Status=Verified                 │
│        + (ExpiresAt is null or ExpiresAt > nowUtc)                │
└──────────────────────────┬──────────────────────────────────────┘
                           │
              found ──────► Allow
              not found ──► fall through to next capability source
└─────────────────────────────────────────────────────────────────┘
```
