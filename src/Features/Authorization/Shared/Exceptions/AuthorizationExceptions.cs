namespace ShapeUp.Features.Authorization.Shared.Exceptions;

public class AuthorizationException : Exception
{
    public AuthorizationException(string message) : base(message)
    {
    }

    public AuthorizationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

public class UserNotFoundException : AuthorizationException
{
    public UserNotFoundException(string firebaseUid) : base($"User with Firebase UID '{firebaseUid}' not found.")
    {
    }
}

public class GroupNotFoundException : AuthorizationException
{
    public GroupNotFoundException(int groupId) : base($"Group with ID {groupId} not found.")
    {
    }
}

public class ScopeNotFoundException : AuthorizationException
{
    public ScopeNotFoundException(int scopeId) : base($"Scope with ID {scopeId} not found.")
    {
    }

    public ScopeNotFoundException(string scopeName) : base($"Scope '{scopeName}' not found.")
    {
    }
}

public class UnauthorizedException : AuthorizationException
{
    public UnauthorizedException(string message) : base(message)
    {
    }
}

public class InsufficientScopesException : UnauthorizedException
{
    public InsufficientScopesException(string requiredScopes) 
        : base($"User does not have the required scopes: {requiredScopes}")
    {
    }
}

