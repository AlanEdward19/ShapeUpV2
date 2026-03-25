namespace ShapeUp.Features.Authorization.Scopes.CreateScope;

public record CreateScopeCommand(string Domain, string Subdomain, string Action, string? Description);

