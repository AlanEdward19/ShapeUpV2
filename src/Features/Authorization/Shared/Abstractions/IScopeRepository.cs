namespace ShapeUp.Features.Authorization.Shared.Abstractions;

using Entities;

public interface IScopeRepository
{
    Task<Scope?> GetByIdAsync(int scopeId, CancellationToken cancellationToken);
    Task<Scope?> GetByNameAsync(string scopeName, CancellationToken cancellationToken);
    Task<Scope?> GetByScopeFormatAsync(string domain, string subdomain, string action, CancellationToken cancellationToken);
    Task<IReadOnlyList<Scope>> GetAllAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<Scope>> GetAllKeysetAsync(int? lastScopeId, int pageSize, CancellationToken cancellationToken);
    Task<IReadOnlyList<Scope>> GetUserScopesAsync(int userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Scope>> GetUserScopesKeysetAsync(int userId, int? lastScopeId, int pageSize, CancellationToken cancellationToken);
    Task<IReadOnlyList<Scope>> GetGroupScopesAsync(int groupId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Scope>> GetByDomainAsync(string domain, CancellationToken cancellationToken);
    Task AddAsync(Scope scope, CancellationToken cancellationToken);
    Task UpdateAsync(Scope scope, CancellationToken cancellationToken);
    Task DeleteAsync(int scopeId, CancellationToken cancellationToken);
    Task AssignScopeToUserAsync(int userId, int scopeId, CancellationToken cancellationToken);
    Task RemoveScopeFromUserAsync(int userId, int scopeId, CancellationToken cancellationToken);
    Task AssignScopeToGroupAsync(int groupId, int scopeId, CancellationToken cancellationToken);
    Task RemoveScopeFromGroupAsync(int groupId, int scopeId, CancellationToken cancellationToken);
}

