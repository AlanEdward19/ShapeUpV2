using Microsoft.EntityFrameworkCore;
using ShapeUp.Features.AuditLogs.Shared.Entities;
using ShapeUp.Features.Authorization.Shared.Data;
using ShapeUp.Features.Authorization.Shared.Entities;

namespace IntegrationTests.Infrastructure;

public static class TestDataSeeder
{
    public static async Task<User> SeedUserAsync(AuthorizationDbContext context, string suffix, CancellationToken cancellationToken)
    {
        var user = new User
        {
            FirebaseUid = $"uid-{suffix}",
            Email = $"{suffix}@integration.test",
            IsActive = true
        };

        context.Users.Add(user);
        await context.SaveChangesAsync(cancellationToken);
        return user;
    }

    public static async Task<Group> SeedGroupAsync(AuthorizationDbContext context, string name, int createdById, CancellationToken cancellationToken)
    {
        var group = new Group
        {
            Name = name,
            CreatedById = createdById
        };

        context.Groups.Add(group);
        await context.SaveChangesAsync(cancellationToken);
        return group;
    }

    public static async Task<Scope> SeedScopeAsync(AuthorizationDbContext context, string domain, string subdomain, string action, CancellationToken cancellationToken)
    {
        var scope = new Scope
        {
            Name = $"{domain}:{subdomain}:{action}",
            Domain = domain,
            Subdomain = subdomain,
            Action = action,
            Description = "integration"
        };

        context.Scopes.Add(scope);
        await context.SaveChangesAsync(cancellationToken);
        return scope;
    }

    public static async Task AssignScopesToUserAsync(AuthorizationDbContext context, int userId, params string[] scopeNames)
    {
        var scopes = await context.Scopes.Where(s => scopeNames.Contains(s.Name)).ToListAsync();
        foreach (var scope in scopes)
        {
            context.UserScopes.Add(new UserScope { UserId = userId, ScopeId = scope.Id });
        }

        await context.SaveChangesAsync();
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

