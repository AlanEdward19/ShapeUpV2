namespace ShapeUp.Features.Authorization.Scopes.GetScopes;

public record GetScopesResponse(int ScopeId, string Name, string Domain, string Subdomain, string Action, string? Description);