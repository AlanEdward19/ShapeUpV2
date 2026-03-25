namespace ShapeUp.Features.Authorization.Scopes.CreateScope;

public record CreateScopeResponse(int ScopeId, string Name, string Domain, string Subdomain, string Action, string? Description);