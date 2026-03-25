namespace ShapeUp.Features.Authorization.Shared.Errors;

using ShapeUp.Shared.Results;

public static class AuthorizationErrors
{
    public static Error GroupNotFound(int groupId) =>
        CommonErrors.NotFound($"Group with ID {groupId} was not found.");

    public static Error UserNotFound(int userId) =>
        CommonErrors.NotFound($"User with ID {userId} was not found.");

    public static Error ScopeNotFound(int scopeId) =>
        CommonErrors.NotFound($"Scope with ID {scopeId} was not found.");

    public static Error InvalidRole(string role) =>
        CommonErrors.Validation($"Invalid group role '{role}'.");

    public static Error GroupMemberAlreadyExists(int userId, int groupId) =>
        CommonErrors.Conflict($"User {userId} is already a member of group {groupId}.");

    public static Error GroupMemberNotFound(int userId, int groupId) =>
        CommonErrors.NotFound($"User {userId} is not a member of group {groupId}.");

    public static Error MissingPermission(string message) =>
        CommonErrors.Forbidden(message);

    public static Error ScopeAlreadyExists(string scopeName) =>
        CommonErrors.Conflict($"Scope '{scopeName}' already exists.");
}

