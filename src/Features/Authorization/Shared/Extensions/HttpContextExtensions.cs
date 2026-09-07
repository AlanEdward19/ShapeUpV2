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

}

