# ShapeUp Authorization Domain

## Overview

The Authorization domain provides a complete system for managing users, groups, and scopes with Firebase integration. It includes:

- **User Management**: Auto-provisioning users from Firebase tokens
- **Groups**: Create groups, manage members with roles (Owner, Administrator, Member)
- **Scopes**: Define fine-grained permissions in format `domain:subdomain:action`
- **Authorization**: Protect routes with scope-based access control
- **Firebase Sync**: Automatically sync user scopes to Firebase custom claims

## Scope Format

Scopes follow the format: `domain:subdomain:action`

Examples:
- `groups:management:create` - Create groups
- `groups:management:delete` - Delete groups
- `groups:management:manage_members` - Add/remove members and update roles
- `groups:management:manage_scopes` - Assign scopes to groups
- `scopes:management:create` - Create scopes
- `users:profile:read` - Read user profile
- `users:profile:update` - Update user profile

## API Endpoints

### User Management

#### Get or Create User
```http
POST /api/users/get-or-create
Authorization: Bearer {token}
Content-Type: application/json

{
  "firebaseUid": "firebase-user-uid",
  "email": "user@example.com",
  "displayName": "User Name"
}
```

#### Logout (Revoke Firebase Refresh Tokens)
```http
POST /api/users/logout
Authorization: Bearer {token}
```

Revokes refresh tokens for the authenticated Firebase user. Existing ID tokens can remain valid until expiration.

### Groups

#### Create Group
```http
POST /api/groups
Authorization: Bearer {token}
Content-Type: application/json

{
  "name": "Development Team",
  "description": "Team for development tasks"
}
```

**Required Scope**: `groups:management:create`

#### Get User's Groups
```http
GET /api/groups
Authorization: Bearer {token}
```

#### Get Group by ID
```http
GET /api/groups/{groupId}
Authorization: Bearer {token}
```

#### Add User to Group
```http
POST /api/groups/{groupId}/members
Authorization: Bearer {token}
Content-Type: application/json

{
  "userId": 5,
  "role": "Member"  // Owner, Administrator, or Member
}
```

**Required Scope**: `groups:management:manage_members`

#### Update User Role in Group
```http
PUT /api/groups/{groupId}/members/{userId}/role
Authorization: Bearer {token}
Content-Type: application/json

{
  "newRole": "Administrator"
}
```

**Required Scope**: `groups:management:manage_members`

#### Remove User from Group
```http
DELETE /api/groups/{groupId}/members/{userId}
Authorization: Bearer {token}
```

**Required Scope**: `groups:management:manage_members`

#### Delete Group (Owner only)
```http
DELETE /api/groups/{groupId}
Authorization: Bearer {token}
```

**Required Scope**: `groups:management:delete`

#### Assign Scope to Group
```http
POST /api/groups/{groupId}/scopes
Authorization: Bearer {token}
Content-Type: application/json

{
  "scopeId": 3
}
```

**Required Scope**: `groups:management:manage_scopes`

### Scopes

#### Create Scope
```http
POST /api/scopes
Authorization: Bearer {token}
Content-Type: application/json

{
  "domain": "groups",
  "subdomain": "management",
  "action": "create",
  "description": "Allows creation of groups"
}
```

**Required Scope**: `scopes:management:create`

#### Get All Scopes
```http
GET /api/scopes
Authorization: Bearer {token}
```

#### Get User's Scopes (direct + inherited from groups)
```http
GET /api/scopes/user/{userId}
Authorization: Bearer {token}
```

#### Assign Scope to User
```http
POST /api/scopes/assign-to-user/{userId}
Authorization: Bearer {token}
Content-Type: application/json

{
  "scopeId": 1
}
```

#### Remove Scope from User
```http
DELETE /api/scopes/remove-from-user/{userId}
Authorization: Bearer {token}
Content-Type: application/json

{
  "scopeId": 1
}
```

## Protecting Routes

Use the `RequireScopes` attribute to protect your endpoints:

```csharp
[ApiController]
[Route("api/products")]
public class ProductController : ControllerBase
{
    [HttpPost]
    [TypeFilter(typeof(RequireScopesAttribute), Arguments = new object[] { new[] { "products:management:create" } })]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request)
    {
        var userId = HttpContext.GetUserId();
        var userScopes = HttpContext.GetUserScopes();
        
        // Your logic here
        return Ok(response);
    }

    [HttpDelete("{productId}")]
    [TypeFilter(typeof(RequireScopesAttribute), Arguments = new object[] { new[] { "products:management:delete" } })]
    public async Task<IActionResult> DeleteProduct(int productId)
    {
        var userId = HttpContext.GetUserId();
        
        // Your logic here
        return Ok();
    }
}
```

## Helper Extensions

Access user context in your handlers:

```csharp
// Get current user ID
var userId = HttpContext.GetUserId();

// Get user context (ID + Scopes)
var userContext = HttpContext.GetUserContext();

// Get user's scopes
var scopes = HttpContext.GetUserScopes();

// Check if user has a scope
if (HttpContext.HasScope("groups:management:create"))
{
    // User has this scope
}

// Check if user has all required scopes
if (HttpContext.HasAllScopes("groups:management:create", "groups:management:delete"))
{
    // User has all scopes
}

// Check if user has any of the scopes
if (HttpContext.HasAnyScope("admin:full", "groups:management:create"))
{
    // User has at least one scope
}
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

5. Set the `GOOGLE_APPLICATION_CREDENTIALS` environment variable to point to your service account key file

## Key Features

### Automatic User Provisioning
When a user authenticates via Firebase and sends their token to the API:
1. Token is verified with Firebase
2. User is automatically created in the database if not exists
3. User's scopes are retrieved (direct + inherited from groups)
4. User context is available in HttpContext for the request

### Scope Synchronization
When a user's scopes change (direct assignment or group membership):
1. Scopes are updated in the local database
2. Firebase custom claims are updated automatically
3. Scopes are deduplicated before returning/storing

### Role-Based Group Management
- **Owner**: Can delete group, manage all members, change roles
- **Administrator**: Can manage members (add/remove) and assign scopes
- **Member**: Can only access group's scopes

## Architecture Decisions

### Scope Format: `domain:subdomain:action`
This hierarchical format allows:
- Easy parsing and validation
- Wildcard support in future (e.g., `groups:*:*`)
- Clear organization of permissions
- Simple string-based comparison

### Direct Scopes + Group Inheritance
Users can have:
1. Direct scopes assigned to them
2. Scopes inherited from all groups they belong to
3. Deduplicated final scope list

### Firebase Custom Claims
User scopes are stored in Firebase custom claims for:
- Offline authorization (client-side validation)
- Consistency across services
- Simplified token payload

## Future Enhancements

1. **Wildcard Scope Matching**: Support `groups:*:*` patterns
2. **Scope Hierarchy**: Parent/child relationships for scopes
3. **Role Templates**: Predefined role-to-scopes mappings
4. **Audit Logging**: Track all authorization changes
5. **Multi-Tenancy**: Tenant-isolated groups and scopes
6. **Caching**: In-memory/distributed cache for scopes
7. **Rate Limiting**: Protect authorization endpoints
8. **API Key Authentication**: Support service-to-service auth

