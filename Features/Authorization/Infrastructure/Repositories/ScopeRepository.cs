namespace ShapeUp.Features.Authorization.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Shared.Abstractions;
using Shared.Data;
using Shared.Entities;

public class ScopeRepository(AuthorizationDbContext context) : IScopeRepository
{
    public async Task<Scope?> GetByIdAsync(int scopeId, CancellationToken cancellationToken) =>
        await context.Scopes
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == scopeId, cancellationToken);

    public async Task<Scope?> GetByNameAsync(string scopeName, CancellationToken cancellationToken) =>
        await context.Scopes
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Name == scopeName, cancellationToken);

    public async Task<Scope?> GetByScopeFormatAsync(string domain, string subdomain, string action, CancellationToken cancellationToken) =>
        await context.Scopes
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Domain == domain && s.Subdomain == subdomain && s.Action == action, cancellationToken);

    public async Task<IReadOnlyList<Scope>> GetAllAsync(CancellationToken cancellationToken) =>
        await context.Scopes
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Scope>> GetAllKeysetAsync(int? lastScopeId, int pageSize, CancellationToken cancellationToken)
    {
        var query = context.Scopes.AsQueryable();

        if (lastScopeId.HasValue)
            query = query.Where(s => s.Id < lastScopeId.Value);

        return await query
            .OrderByDescending(s => s.Id)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Scope>> GetUserScopesAsync(int userId, CancellationToken cancellationToken)
    {
        // Get direct user scopes + scopes from groups
        var directScopes = await context.UserScopes
            .Where(us => us.UserId == userId)
            .Include(us => us.Scope)
            .Select(us => us.Scope!)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var groupScopes = await context.UserGroups
            .Where(ug => ug.UserId == userId)
            .SelectMany(ug => ug.Group!.Scopes.Select(gs => gs.Scope!))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // Combine and deduplicate by scope ID
        return directScopes
            .Concat(groupScopes)
            .DistinctBy(s => s.Id)
            .ToList();
    }

    public async Task<IReadOnlyList<Scope>> GetUserScopesKeysetAsync(int userId, int? lastScopeId, int pageSize, CancellationToken cancellationToken)
    {
        var directScopesQuery = context.UserScopes
            .Where(us => us.UserId == userId)
            .Select(us => us.Scope!);

        var groupScopesQuery = context.UserGroups
            .Where(ug => ug.UserId == userId)
            .SelectMany(ug => ug.Group!.Scopes.Select(gs => gs.Scope!));

        var mergedQuery = directScopesQuery
            .Union(groupScopesQuery)
            .Distinct();

        if (lastScopeId.HasValue)
            mergedQuery = mergedQuery.Where(s => s.Id < lastScopeId.Value);

        return await mergedQuery
            .OrderByDescending(s => s.Id)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Scope>> GetGroupScopesAsync(int groupId, CancellationToken cancellationToken) =>
        await context.GroupScopes
            .Where(gs => gs.GroupId == groupId)
            .Include(gs => gs.Scope)
            .Select(gs => gs.Scope!)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Scope>> GetByDomainAsync(string domain, CancellationToken cancellationToken) =>
        await context.Scopes
            .Where(s => s.Domain == domain)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Scope scope, CancellationToken cancellationToken)
    {
        context.Scopes.Add(scope);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Scope scope, CancellationToken cancellationToken)
    {
        context.Scopes.Update(scope);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int scopeId, CancellationToken cancellationToken)
    {
        await context.Scopes
            .Where(s => s.Id == scopeId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task AssignScopeToUserAsync(int userId, int scopeId, CancellationToken cancellationToken)
    {
        var userScope = new UserScope { UserId = userId, ScopeId = scopeId };
        context.UserScopes.Add(userScope);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveScopeFromUserAsync(int userId, int scopeId, CancellationToken cancellationToken)
    {
        await context.UserScopes
            .Where(us => us.UserId == userId && us.ScopeId == scopeId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task AssignScopeToGroupAsync(int groupId, int scopeId, CancellationToken cancellationToken)
    {
        var groupScope = new GroupScope { GroupId = groupId, ScopeId = scopeId };
        context.GroupScopes.Add(groupScope);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveScopeFromGroupAsync(int groupId, int scopeId, CancellationToken cancellationToken)
    {
        await context.GroupScopes
            .Where(gs => gs.GroupId == groupId && gs.ScopeId == scopeId)
            .ExecuteDeleteAsync(cancellationToken);
    }
}

