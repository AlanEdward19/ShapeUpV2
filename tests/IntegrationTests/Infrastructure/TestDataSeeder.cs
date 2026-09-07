using Microsoft.EntityFrameworkCore;
using ShapeUp.Features.AuditLogs.Shared.Entities;
using ShapeUp.Features.Authorization.Shared.Data;
using ShapeUp.Features.Authorization.Shared.Entities;
using ShapeUp.Features.GymManagement.Infrastructure.Data;
using ShapeUp.Features.GymManagement.Shared.Entities;

namespace IntegrationTests.Infrastructure;

public static class TestDataSeeder
{
    public static async Task<User> SeedUserAsync(AuthorizationDbContext context, string suffix, CancellationToken cancellationToken)
    {
        var firebaseUid = $"uid-{suffix}";
        var existing = await context.Users.FirstOrDefaultAsync(u => u.FirebaseUid == firebaseUid, cancellationToken);
        if (existing is not null)
            return existing;

        var user = new User
        {
            FirebaseUid = firebaseUid,
            Email = $"{suffix}@integration.test",
            IsActive = true
        };

        context.Users.Add(user);
        await context.SaveChangesAsync(cancellationToken);
        return user;
    }

    /// <summary>
    /// Grants PlatformRoleType.Admin (native-authorization-model AD-003/AD-006 -- the
    /// "capability:platform.*" gate) so the actor can call platform-admin-only endpoints
    /// (exercise/equipment catalog, platform tiers, user role assignment) over real HTTP.
    /// </summary>
    public static async Task GrantPlatformAdminAsync(GymManagementDbContext context, int userId, CancellationToken cancellationToken)
    {
        var existing = await context.UserPlatformRoles
            .FirstOrDefaultAsync(r => r.UserId == userId && r.Role == PlatformRoleType.Admin, cancellationToken);
        if (existing is not null)
        {
            existing.IsActive = true;
            await context.SaveChangesAsync(cancellationToken);
            return;
        }

        context.UserPlatformRoles.Add(new UserPlatformRole { UserId = userId, Role = PlatformRoleType.Admin, IsActive = true });
        await context.SaveChangesAsync(cancellationToken);
    }

    public static AuditLogEntry BuildAuditEntry(string method, string endpoint, string? email, int statusCode) => new()
    {
        OccurredAtUtc = DateTime.UtcNow,
        HttpMethod = method,
        Endpoint = endpoint,
        UserEmail = email,
        StatusCode = statusCode,
        DurationMs = 10,
        TraceId = Guid.NewGuid().ToString("N")
    };
}

