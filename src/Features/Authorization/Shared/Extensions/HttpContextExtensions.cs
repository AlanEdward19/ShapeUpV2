namespace ShapeUp.Features.Authorization.Shared.Extensions;

using ShapeUp.Features.Authorization.Infrastructure.Authorization;

public static class HttpContextExtensions
{
    /// <summary>
    /// Gets the current user context from HttpContext.
    /// </summary>
    public static UserContext? GetUserContext(this HttpContext context)
    {
        return context.Items.TryGetValue("User", out var user) ? user as UserContext : null;
    }

    /// <summary>
    /// Gets the current user ID from HttpContext.
    /// </summary>
    public static int GetUserId(this HttpContext context)
    {
        if (context.Items.TryGetValue("UserId", out var userId) && userId is int id)
            return id;
        throw new InvalidOperationException("User context not found. Ensure AuthorizationMiddleware is registered.");
    }

    /// <summary>
    /// Gets the current user's scopes from HttpContext.
    /// </summary>
    public static string[] GetUserScopes(this HttpContext context)
    {
        var userContext = context.GetUserContext();
        if (userContext == null)
            throw new InvalidOperationException("User context not found. Ensure AuthorizationMiddleware is registered.");
        return userContext.Scopes;
    }

    /// <summary>
    /// Checks if the current user has a specific scope.
    /// </summary>
    public static bool HasScope(this HttpContext context, string scope)
    {
        var userScopes = context.GetUserScopes();
        return userScopes.Contains(scope);
    }

    /// <summary>
    /// Checks if the current user has all required scopes.
    /// </summary>
    public static bool HasAllScopes(this HttpContext context, params string[] requiredScopes)
    {
        var userScopes = new HashSet<string>(context.GetUserScopes());
        return requiredScopes.All(scope => userScopes.Contains(scope));
    }

    /// <summary>
    /// Checks if the current user has any of the required scopes.
    /// </summary>
    public static bool HasAnyScope(this HttpContext context, params string[] requiredScopes)
    {
        var userScopes = new HashSet<string>(context.GetUserScopes());
        return requiredScopes.Any(scope => userScopes.Contains(scope));
    }
}

