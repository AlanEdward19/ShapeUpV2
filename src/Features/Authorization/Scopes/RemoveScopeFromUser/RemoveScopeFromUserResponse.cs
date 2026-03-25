namespace ShapeUp.Features.Authorization.Scopes.RemoveScopeFromUser;

public record RemoveScopeFromUserResponse(int UserId, int ScopeId, string Message);