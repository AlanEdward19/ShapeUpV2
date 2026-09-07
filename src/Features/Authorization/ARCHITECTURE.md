# Authorization Domain - Architecture & Implementation

## Domain Scope

The `Authorization` domain is responsible for identity-context provisioning and for hosting the
native capability-based authorization pipeline used across the whole API (RFC-001,
native-authorization-model).

Core responsibilities:
- Authenticate requests via Firebase token verification (Firebase remains the sole
  authentication mechanism -- unchanged by RFC-001).
- Provision users automatically on first valid request.
- Assign default `IndependentClient` platform role when a new user is provisioned.
- Store minimal per-request identity context (`UserContext`) for handlers/controllers to read.
- Host the native ASP.NET Core policy-based authorization pipeline (`CapabilityPolicyProvider`,
  `CapabilityAuthorizationHandler`, `CapabilityResolver`, `CapabilityAuthorizationResultHandler`)
  that every domain's `[Authorize(Policy = "capability:...")]` attributes resolve through.

> The legacy `Group`/`Scope`/`UserGroup`/`GroupScope` RBAC model (permission strings synced to
> Firebase custom claims) has been fully removed. See `docs/rfcs/rfc-001-authorization-model.md`
> for the decision record and `.specs/features/native-authorization-model/` for the spec/design
> that replaced it.

## Domain Structure

```text
Features/Authorization/
├── Shared/
│   ├── Entities/
│   │   └── User.cs
│   ├── Data/
│   │   └── AuthorizationDbContext.cs
│   ├── Abstractions/
│   │   ├── IUserRepository.cs
│   │   └── IFirebaseService.cs
│   ├── Errors/
│   │   └── AuthorizationErrors.cs
│   ├── Exceptions/
│   │   └── AuthorizationExceptions.cs
│   └── Extensions/
│       └── HttpContextExtensions.cs
├── Infrastructure/
│   ├── Repositories/
│   │   └── UserRepository.cs
│   ├── Firebase/
│   │   └── FirebaseService.cs
│   └── Authorization/
│       └── AuthorizationMiddleware.cs
├── Resolver/
│   ├── ICapabilityResolver.cs
│   ├── CapabilityResolver.cs
│   ├── CapabilityAuthorizationHandler.cs
│   ├── CapabilityAuthorizationResultHandler.cs
│   ├── CapabilityPolicyProvider.cs
│   ├── CapabilityRequirement.cs
│   ├── CapabilityResult.cs
│   ├── AuthorizationContext.cs
│   ├── IAuthorizationAuditWriter.cs
│   └── AuthorizationAuditWriter.cs
├── UserManagement/
│   ├── GetUser/
│   ├── RevokeCurrentToken/
│   └── UserManagementController.cs
├── README.md
├── EXAMPLES.http
└── ARCHITECTURE.md
```

## Database Structure

### Entities
- `User`: platform user linked to Firebase (`FirebaseUid`, `Email`, status, audit timestamps).
  No `Groups`/`Scopes` navigation properties -- those were removed with the legacy RBAC tables.

### Relational Schema

```text
Users
├── Id (PK)
├── FirebaseUid (UK)
├── Email (UK)
├── DisplayName
├── CreatedAt
├── UpdatedAt
└── IsActive
```

`AuthorizationDbContext` now only maps `Users`. The `Groups`, `UserGroups`, `Scopes`,
`UserScopes`, `GroupScopes` tables were dropped by migration
`20260907021112_RemoveLegacyGroupScopeRbac`.

## Capability Sources (RFC-001)

Authorization decisions are made by `CapabilityResolver`, which checks up to five independent
sources for a given `(userId, capability, AuthorizationContext)`. The context is built by
`CapabilityAuthorizationHandler` from the request's route values before the resolver ever runs.

1. **Platform admin** (`AuthorizationContext.RequiresPlatformAdmin`) -- exclusive gate. Any
   policy name prefixed `"platform."` (e.g. `capability:platform.audit_logs.read`) is inferred by
   the handler to require `PlatformRoleType.Admin` (`GymManagement.UserPlatformRoles`),
   regardless of route values. Never falls through to the other sources.
2. **Self-access** -- `AuthorizationContext.TargetUserId == userId`. A user always has the
   capability to act on their own data.
3. **Organization membership** (`IOrganizationMembershipRepository`) -- adapter over
   `GymManagement` (`Gym.OwnerId`, `GymStaff`); resolves by `GymId` in the route.
4. **Professional-client relationship** (`IProfessionalClientRelationshipRepository`) -- new
   table (`Relationships` domain); resolves by `(TargetUserId, RelationshipType)`.
5. **Professional credential** (`IProfessionalCredentialRepository`) -- new table
   (`Credentials` domain); resolves by `RequiredProfessionType`.
6. **Entitlement** (`IEntitlementRepository`) -- adapter over `GymManagement.PlatformTier`;
   resolves by `RequiredEntitlementCapability`.

Resolution is deny-by-default: if no source grants the capability, the result is a deny. Any
infrastructure exception from a repository is left to propagate (never masked as a deny) --
consistent with `AUTHZ-05` in `spec.md`.

### Wiring

```text
CapabilityPolicyProvider : IAuthorizationPolicyProvider
  -> any policy name "capability:<name>" becomes a CapabilityRequirement on the fly
     (no per-capability AddPolicy registration needed)

CapabilityAuthorizationHandler : AuthorizationHandler<CapabilityRequirement>
  -> builds AuthorizationContext from route values (gymId, userId, clientUserId, staffId,
     trainerId) or short-circuits to RequiresPlatformAdmin for "platform."-prefixed capabilities
  -> calls ICapabilityResolver.ResolveAsync(...)

CapabilityAuthorizationResultHandler : IAuthorizationMiddlewareResultHandler
  -> maps any non-succeeded AuthorizationResult straight to 403 Forbidden
     (the framework's default Challenge/Forbid split assumes an AddAuthentication scheme,
     which this API does not register -- Firebase auth is fully custom via
     AuthorizationMiddleware, so Challenge would 500)
```

### Training domain exception (AD-005)

The `Training` domain does **not** use `[Authorize(Policy = "...")]` on its controllers. Its
routes carry the resource id (planId/templateId/sessionId), not the owner id, so a policy handler
cannot build a correct `AuthorizationContext` before the entity is loaded. Instead, authorization
happens inside the command/query handlers via `ITrainingAccessPolicy`, which checks self-access,
then `IProfessionalClientRelationshipRepository`, then the existing `GymManagement`
trainer/client and staff/gym-client links directly. See
`Features/Training/ARCHITECTURE.md` for the full flow.

## Endpoints

### User Management
- `POST /api/users/get-or-create` - get or provision the authenticated user from the Firebase
  bearer token (no request body).
- `GET /api/users/me` - get the authenticated user's own profile. No policy attribute --
  authentication alone is the gate (self-access).
- `GET /api/users/{id}` - get another user's profile.
  `[Authorize(Policy = "capability:platform.users.read")]` -- viewing an arbitrary user by id is
  a platform-admin action.
- `POST /api/users/logout` - revoke refresh tokens for the authenticated Firebase user.

## Authorization Flow (End-to-End)

1. Client sends `Authorization: Bearer {firebaseToken}`.
2. `AuthorizationMiddleware` validates the token using Firebase.
3. Middleware loads or creates the user record in SQL Server.
4. On first provision, middleware assigns the default `IndependentClient` role in
   `GymManagement.UserPlatformRoles`.
5. Middleware stores `UserContext(UserId, FirebaseUid, Email, DisplayName)` in `HttpContext.Items`.
6. ASP.NET Core's policy pipeline runs for any endpoint carrying
   `[Authorize(Policy = "capability:...")]`: `CapabilityPolicyProvider` resolves the requirement,
   `CapabilityAuthorizationHandler` builds the `AuthorizationContext`, `CapabilityResolver`
   decides allow/deny, `CapabilityAuthorizationResultHandler` maps a deny to `403`.
7. Request continues to the handler if authorized (or if the endpoint carries no policy and is
   gated purely by authentication/self-access).
8. Logout endpoint revokes Firebase refresh tokens, invalidating the session on next refresh.

## Dependency Injection View

```text
Program.cs / DependencyInjectionExtensions
├── AuthorizationDbContext
├── IUserRepository -> UserRepository
├── IFirebaseService -> FirebaseService
├── AuthorizationMiddleware
├── IAuthorizationPolicyProvider -> CapabilityPolicyProvider
├── IAuthorizationHandler -> CapabilityAuthorizationHandler
├── IAuthorizationMiddlewareResultHandler -> CapabilityAuthorizationResultHandler
├── ICapabilityResolver -> CapabilityResolver
│   (depends on IOrganizationMembershipRepository, IProfessionalCredentialRepository,
│    IProfessionalClientRelationshipRepository, IEntitlementRepository,
│    IUserPlatformRoleRepository)
└── Feature handlers/controllers that consume authorization context
```

## Security and Operational Notes

- Firebase token validation is the trust gate for identity; this is unchanged by RFC-001 and is
  explicitly out of scope for any future authorization rework (see RFC-001).
- Capability checks default to deny when no source grants the capability.
- Any repository exception during resolution propagates as `500`, never as a silent deny.
- `EXAMPLES.http` is the practical contract reference for manual tests.

## Single Source of Truth

This file is the canonical reference for Authorization domain architecture + implementation
details. See also `docs/rfcs/rfc-001-authorization-model.md` for the decision record and
`.specs/features/native-authorization-model/{spec.md,design.md}` for requirements and design.

## ASCII Diagram

```text
┌─────────────────────────────────────────────────────────────────┐
│                         CLIENT APPLICATION                      │
│                    (Web/Mobile with Firebase)                   │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           │ Firebase Token (JWT)
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│                    ASP.NET CORE API                             │
├─────────────────────────────────────────────────────────────────┤
│  AuthorizationMiddleware                                        │
│  1) Verify Firebase token                                       │
│  2) Get/Create User                                             │
│  3) Store UserContext in HttpContext                            │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│ [Authorize(Policy="capability:...")] (when present)             │
│ CapabilityPolicyProvider -> CapabilityAuthorizationHandler       │
│                          -> CapabilityResolver                  │
│ Sources: platform admin | self-access | membership |            │
│          relationship | credential | entitlement                │
│ CapabilityAuthorizationResultHandler maps deny -> 403            │
└──────────────────────────┬──────────────────────────────────────┘
                           │ authorized
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│               Controllers / Handlers (Vertical Slices)          │
│   (Training domain checks ITrainingAccessPolicy in-handler,     │
│    see AD-005)                                                  │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│ Repositories + AuthorizationDbContext + SQL Server              │
│ Table: Users                                                    │
│ (Membership/Entitlement adapt GymManagement tables;             │
│  Credentials/Relationships own their own tables)                │
└─────────────────────────────────────────────────────────────────┘
```
