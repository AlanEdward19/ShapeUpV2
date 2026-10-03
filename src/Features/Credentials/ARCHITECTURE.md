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

- Receive submissions (`POST /api/credentials`), run them through an `ICredentialVerifier`, and
  let a platform admin approve or reject the ones in the review queue.
- Keep the `Trainer`/`Nutritionist` platform role in step with the credential (see below).

Out of scope: credential issuance integrations and the app screens.

## Domain Structure

```text
Features/Credentials/
├── Shared/
│   ├── Entities/
│   │   ├── ProfessionalCredential.cs
│   │   └── CredentialStatus.cs
│   ├── Abstractions/
│   │   └── IProfessionalCredentialRepository.cs
│   ├── Verification/ICredentialVerifier.cs
│   ├── Lifecycle/ (CredentialLifecycle, ProfessionalRoleGranter)
│   ├── StateMachine/
│   │   └── CredentialStatusGuard.cs
│   └── Data/
│       ├── CredentialsDbContext.cs
│       └── Migrations/
├── Infrastructure/
│   ├── Repositories/
│   │   └── ProfessionalCredentialRepository.cs
│   └── Verification/ManualReviewCredentialVerifier.cs
├── SubmitCredential/ GetMyCredentials/ GetCredentialsUnderReview/ ReviewCredential/ ExpireCredentials/
├── CredentialsController.cs
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
- `VerifiedAt`, `ExpiresAt` (nullable), `SubmittedAt`, `ReviewedAt`, `ReviewedByUserId`
  (null when a verifier decided), `RejectionReason`.

Accepted today: `PersonalTrainer` (authority `CREF`) and `Nutritionist` (authority `CRN`), region = UF,
country = `BR`. Other professions and foreign registrations are rejected by `SubmitCredentialValidator`.

## Endpoints

| Route | Who |
|---|---|
| `POST /api/credentials` | any authenticated user; creates `Submitted` and triggers verification |
| `GET /api/credentials/me` | the owner |
| `GET /api/credentials/under-review`, `POST /{id}/approve`, `POST /{id}/reject` (body `{ reason }`) | platform admin, policy `capability:platform.credentials.review` |

## Verification

`ICredentialVerifier` (`Supports(authority)` + `VerifyAsync`) returns `UnderReview`, `Verified` or
`Rejected`. The only implementation is `ManualReviewCredentialVerifier`: it sends every CREF/CRN
submission to the admin queue. Neither council has a documented public API (the CFN "Consulta
Nacional de Nutricionistas" and the CREFs' searches are web pages), and scraping them needs the terms
of use checked first, so nothing is queried automatically. An automatic verifier is a new
`ICredentialVerifier` registered in `CredentialsModule`; `SubmitCredentialHandler` picks the first
one that supports the authority and falls back to manual review.

## Status changes and platform roles

`ICredentialLifecycle` is the only place the status changes (it uses `CredentialStatusGuard`):

- `Submitted -> UnderReview` right after verification; then `Verified` or `Rejected`.
- `Verified`: `IProfessionalRoleGranter.GrantAsync` creates the role with
  `UserPlatformRole.GrantedByCredentialId = credential.Id`. Idempotent; an existing role (for example
  one assigned by an admin) is left as is.
- `Expired`/`Suspended`/`Revoked` (`EndAsync`): the role is removed only when its
  `GrantedByCredentialId` is this credential. Roles with a null `GrantedByCredentialId` (manual) are
  never removed. If the user has another valid credential for the profession, the role passes to it.

`ExpireCredentialsHostedService` runs `ExpireCredentialsHandler` hourly: `Verified` credentials with
`ExpiresAt <= now` become `Expired`. `GetVerifiedAsync` already ignores them before the job runs.

Not done yet: periodic re-check of verified registrations against the council (needs an automatic
verifier) and an endpoint for admins to suspend or revoke (`EndAsync` already supports it).

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
├── AddScoped<IProfessionalCredentialRepository, ProfessionalCredentialRepository>()
├── AddScoped<ICredentialVerifier, ManualReviewCredentialVerifier>()
├── AddScoped<IProfessionalRoleGranter, ProfessionalRoleGranter>()
├── AddScoped<ICredentialLifecycle, CredentialLifecycle>()
├── handlers and validators
└── AddHostedService<ExpireCredentialsHostedService>()
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
