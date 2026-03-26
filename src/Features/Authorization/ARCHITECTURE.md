# Authorization Domain - Architecture & Implementation

## Domain Scope

The `Authorization` domain is responsible for identity-context provisioning and permission enforcement across the API.

Core responsibilities:
- Authenticate requests via Firebase token verification.
- Provision users automatically on first valid request.
- Manage groups and membership roles (`Owner`, `Administrator`, `Member`).
- Manage permission scopes in `domain:subdomain:action` format.
- Resolve effective user scopes (direct + inherited from groups).
- Enforce endpoint authorization using `RequireScopesAttribute`.
- Synchronize effective scopes to Firebase custom claims.

## Domain Structure

```text
Features/Authorization/
├── Shared/
│   ├── Entities/
│   │   ├── User.cs
│   │   ├── Group.cs
│   │   ├── UserGroup.cs
│   │   ├── Scope.cs
│   │   ├── UserScope.cs
│   │   └── GroupScope.cs
│   ├── Data/
│   │   └── AuthorizationDbContext.cs
│   ├── Abstractions/
│   │   ├── IUserRepository.cs
│   │   ├── IGroupRepository.cs
│   │   ├── IScopeRepository.cs
│   │   └── IFirebaseService.cs
│   ├── Exceptions/
│   │   └── AuthorizationExceptions.cs
│   └── Extensions/
│       └── HttpContextExtensions.cs
├── Infrastructure/
│   ├── Repositories/
│   │   ├── UserRepository.cs
│   │   ├── GroupRepository.cs
│   │   └── ScopeRepository.cs
│   ├── Firebase/
│   │   └── FirebaseService.cs
│   └── Authorization/
│       ├── RequireScopesAttribute.cs
│       └── AuthorizationMiddleware.cs
├── UserManagement/
│   └── GetOrCreateUser/
├── Groups/
│   ├── CreateGroup/
│   ├── GetGroups/
│   ├── AddUserToGroup/
│   ├── RemoveUserFromGroup/
│   ├── UpdateUserRole/
│   ├── DeleteGroup/
│   └── AssignScopeToGroup/
├── Scopes/
│   ├── CreateScope/
│   ├── GetScopes/
│   ├── GetUserScopes/
│   ├── AssignScopeToUser/
│   ├── RemoveScopeFromUser/
│   ├── SyncUserScopes/
│   ├── SyncCurrentUserScopes/
│   └── Shared/
├── README.md
├── EXAMPLES.http
└── ARCHITECTURE.md
```

## Database Structure

### Entities
- `User`: platform user linked to Firebase (`FirebaseUid`, `Email`, status, audit timestamps).
- `Group`: user collection with ownership and membership governance.
- `UserGroup`: many-to-many between users and groups with role metadata.
- `Scope`: permission unit (`Name`, `Domain`, `Subdomain`, `Action`).
- `UserScope`: direct scope assignment to a user.
- `GroupScope`: scope assignment to a group.

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

Groups
├── Id (PK)
├── Name
├── Description
├── CreatedById (FK -> Users)
├── CreatedAt
└── UpdatedAt

UserGroups
├── UserId (PK, FK -> Users)
├── GroupId (PK, FK -> Groups)
├── Role (Owner|Administrator|Member)
└── JoinedAt

Scopes
├── Id (PK)
├── Name (UK)
├── Domain
├── Subdomain
├── Action
├── Description
└── CreatedAt

UserScopes
├── UserId (PK, FK -> Users)
├── ScopeId (PK, FK -> Scopes)
└── AssignedAt

GroupScopes
├── GroupId (PK, FK -> Groups)
├── ScopeId (PK, FK -> Scopes)
└── AssignedAt
```

## Endpoints

### User Management
- `POST /api/users/get-or-create` - get or provision the authenticated user from the Firebase bearer token (no request body).
- `POST /api/users/logout` - revoke refresh tokens for the authenticated Firebase user.

### Groups
- `POST /api/groups` - create group (requires `groups:management:create`).
- `GET /api/groups` - list user groups.
- `GET /api/groups/{groupId}` - get group details and members.
- `POST /api/groups/{groupId}/members` - add member (requires `groups:management:manage_members`).
- `PUT /api/groups/{groupId}/members/{userId}/role` - update member role (requires `groups:management:manage_members`).
- `DELETE /api/groups/{groupId}/members/{userId}` - remove member (requires `groups:management:manage_members`).
- `POST /api/groups/{groupId}/scopes` - assign scope to group (requires `groups:management:manage_scopes`).
- `DELETE /api/groups/{groupId}` - delete group (owner + `groups:management:delete`).

### Scopes
- `POST /api/scopes` - create scope (requires `scopes:management:create`).
- `GET /api/scopes` - list available scopes.
- `GET /api/scopes/user/{userId}` - get effective user scopes (direct + inherited).
- `POST /api/scopes/assign-to-user/{userId}` - assign scope to user.
- `DELETE /api/scopes/remove-from-user/{userId}` - remove scope from user.
- `POST /api/scopes/sync/user/{userId}` - synchronize effective user scopes to Firebase custom claims (requires `scopes:management:sync`).
- `POST /api/scopes/sync/me` - synchronize the authenticated user's effective scopes to Firebase custom claims.

## Authorization Flow (End-to-End)

1. Client sends `Authorization: Bearer {firebaseToken}`.
2. `AuthorizationMiddleware` validates token using Firebase.
3. Middleware loads or creates user record in SQL Server.
4. Middleware resolves effective scopes from `UserScopes` + `GroupScopes`.
5. Middleware stores user context in `HttpContext.Items`.
6. Endpoint filters (`RequireScopesAttribute`) validate required scopes.
7. Request continues to handler if authorized; otherwise returns `403`.
8. Scope updates trigger Firebase custom claims synchronization.
9. Logout endpoint revokes Firebase refresh tokens for current user session invalidation on refresh.

## Key Behavior and Rules

### Role Model
```csharp
public enum GroupRole
{
    Owner = 0,
    Administrator = 1,
    Member = 2
}
```

### Scope Format
- Canonical format: `domain:subdomain:action`.
- Examples: `groups:management:create`, `users:profile:read`.
- Seeded sync permission: `scopes:management:sync`.

### Effective Scopes
- Effective scopes are computed as:
  - direct user scopes + all scopes from groups where user is a member.
- Final output is deduplicated before use and sync.

## Dependency Injection View

```text
Program.cs
├── AuthorizationDbContext
├── IUserRepository -> UserRepository
├── IGroupRepository -> GroupRepository
├── IScopeRepository -> ScopeRepository
├── IFirebaseService -> FirebaseService
├── AuthorizationMiddleware
├── RequireScopesAttribute
└── Feature handlers/controllers that consume authorization context
```

## Security and Operational Notes

- Firebase token validation is the trust gate for identity.
- Scope checks default to deny when requirements are not met.
- Database constraints enforce relationship integrity.
- Firebase custom claims remain aligned with server-side scope state.
- `EXAMPLES.http` is the practical contract reference for manual tests.

## Single Source of Truth

This file is the canonical reference for Authorization domain architecture + implementation details.

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
│  3) Resolve effective scopes                                    │
│  4) Store UserContext in HttpContext                            │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│                 RequireScopesAttribute                          │
│                 Validate required scopes                        │
└──────────────────────────┬──────────────────────────────────────┘
                           │ authorized
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│               Controllers / Handlers (Vertical Slices)          │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│ Repositories + AuthorizationDbContext + SQL Server              │
│ Tables: Users, Groups, UserGroups, Scopes, UserScopes,          │
│         GroupScopes                                              │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│ Firebase Custom Claims Sync (effective scopes)                  │
└─────────────────────────────────────────────────────────────────┘
```
