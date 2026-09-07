# ShapeUp Authorization Domain

## Overview

The Authorization domain provides Firebase-backed identity provisioning and hosts the native,
capability-based authorization pipeline used across the whole API (RFC-001,
native-authorization-model). It includes:

- **User Management**: Auto-provisioning users from Firebase tokens.
- **Native capability authorization**: `[Authorize(Policy = "capability:<name>")]` resolved
  through `CapabilityPolicyProvider` / `CapabilityAuthorizationHandler` / `CapabilityResolver`.
- **Firebase Auth**: Firebase remains the sole authentication mechanism (unchanged, out of
  scope for RFC-001).

> The previous Group/Scope RBAC model described in earlier revisions of this file (permission
> strings in `domain:subdomain:action` format, synced to Firebase custom claims) has been fully
> removed. See `docs/rfcs/rfc-001-authorization-model.md` for why, and
> `Features/Authorization/ARCHITECTURE.md` for the current capability model in detail.

## API Endpoints

### User Management

#### Get or Create User
```http
POST /api/users/get-or-create
Authorization: Bearer {token}
```

The API derives `firebaseUid`, `email`, and `displayName` from the authenticated Firebase token
validated by the authorization middleware.

#### Get Current User
```http
GET /api/users/me
Authorization: Bearer {token}
```

No policy attribute -- authentication alone is the gate (self-access).

#### Get User by Id (platform admin)
```http
GET /api/users/{id}
Authorization: Bearer {token}
```

**Required Policy**: `capability:platform.users.read` (requires `PlatformRoleType.Admin`).

#### Logout (Revoke Firebase Refresh Tokens)
```http
POST /api/users/logout
Authorization: Bearer {token}
```

Revokes refresh tokens for the authenticated Firebase user. Existing ID tokens can remain valid
until expiration.

## Protecting Routes

Use `[Authorize(Policy = "capability:<name>")]` to protect endpoints. No per-capability
registration is needed -- `CapabilityPolicyProvider` builds the policy dynamically for any
policy name prefixed `capability:`.

```csharp
[ApiController]
[Route("api/products")]
public class ProductController : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = "capability:platform.products.manage")]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request)
    {
        var userId = HttpContext.GetUserId();

        // Your logic here
        return Ok(response);
    }
}
```

Capability names prefixed `"platform."` always require `PlatformRoleType.Admin`, independent of
route values. Any other capability is resolved against the request's route values (`gymId`,
`userId`, `clientUserId`, `staffId`, `trainerId`) by `CapabilityAuthorizationHandler` -- see
`ARCHITECTURE.md` for the full list of capability sources.

For domains where the route carries a resource id rather than an owner id (e.g. `Training`),
`[Authorize(Policy = ...)]` on the controller cannot build a correct context. Those domains
authorize inside the handler instead -- see `Features/Training/ARCHITECTURE.md` (AD-005).

## Helper Extensions

Access user context in your handlers:

```csharp
// Get current user ID
var userId = HttpContext.GetUserId();

// Get user context (UserId, FirebaseUid, Email, DisplayName)
var userContext = HttpContext.GetUserContext();
```

## Database Setup

### Create Database
```bash
dotnet ef migrations add InitializeAuthorization --context AuthorizationDbContext
dotnet ef database update --context AuthorizationDbContext
```

### Connection String
Update `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=ShapeUpDb;User Id=YOUR_USER;Password=YOUR_PASSWORD;"
  }
}
```

## Firebase Setup

1. Create a Firebase project in Firebase Console
2. Enable Authentication (Email/Password and Google Sign-In)
3. Create a service account key (Project Settings > Service Accounts > Generate new private key)
4. Update `appsettings.json` with your Firebase project ID:

```json
{
  "Firebase": {
    "ProjectId": "your-firebase-project-id"
  }
}
```

5. Set the `GOOGLE_APPLICATION_CREDENTIALS` environment variable to point to your service account
   key file

## Key Features

### Automatic User Provisioning
When a user authenticates via Firebase and sends their token to the API:
1. Token is verified with Firebase.
2. User is automatically created in the database if not exists.
3. Default `IndependentClient` platform role is assigned on first provision.
4. `UserContext` is available in `HttpContext` for the request.

### Capability Resolution
When an endpoint carries `[Authorize(Policy = "capability:<name>")]`:
1. `CapabilityAuthorizationHandler` builds an `AuthorizationContext` from route values (or infers
   `RequiresPlatformAdmin` from a `"platform."` prefix).
2. `CapabilityResolver` checks platform-admin, self-access, organization membership,
   professional-client relationship, professional credential, and entitlement, in that order,
   returning on the first match.
3. A deny is mapped straight to `403` by `CapabilityAuthorizationResultHandler`.

## Architecture Decisions

See `Features/Authorization/ARCHITECTURE.md` for the full capability-source model and
`docs/rfcs/rfc-001-authorization-model.md` for the decision record (why Group/Scope RBAC was
replaced, why Firebase Auth was kept unchanged, and the alternatives considered).

## Future Enhancements

1. **Model GymManagement's compound staff/gym-client relationship in `CapabilityResolver`**
   instead of leaving it as an unconditional check inside `ITrainingAccessPolicy` (see AD-005 in
   `.specs/features/native-authorization-model/STATE.md`).
2. **Audit Logging**: `AuthorizationAuditWriter` currently exists; extend coverage as new
   capabilities are added.
3. **Rate Limiting**: Protect authorization-sensitive endpoints.
