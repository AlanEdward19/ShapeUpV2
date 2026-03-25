namespace ShapeUp.Features.Authorization.Scopes.AssignScopeToUser;

public record AssignScopeToUserResponse(int UserId, int ScopeId, string ScopeName);