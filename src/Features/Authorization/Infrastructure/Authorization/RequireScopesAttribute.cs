namespace ShapeUp.Features.Authorization.Infrastructure.Authorization;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ShapeUp.Shared.Results;

/// <summary>
/// Attribute to require specific scopes for accessing an endpoint.
/// Usage: [RequireScopes("groups:management:create", "users:profile:read")]
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireScopesAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly string[] _requiredScopes;

    public RequireScopesAttribute(params string[] requiredScopes)
    {
        _requiredScopes = requiredScopes;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // Get user from context (set by AuthorizationMiddleware)
        if (!context.HttpContext.Items.TryGetValue("User", out var userObj) || userObj is not UserContext user)
        {
            var error = CommonErrors.Unauthorized("User context not found.");
            context.Result = new ObjectResult(new { error.Code, error.Message })
            {
                StatusCode = error.StatusCode
            };
            return;
        }

        // Check if user has all required scopes
        var userScopes = new HashSet<string>(user.Scopes);
        var hasAllScopes = _requiredScopes.All(scope => userScopes.Contains(scope));

        if (!hasAllScopes)
        {
            var error = CommonErrors.Forbidden("Missing required scopes.");
            context.Result = new ObjectResult(new { error.Code, error.Message })
            {
                StatusCode = error.StatusCode
            };
            return;
        }

        // Authorization successful
        await Task.CompletedTask;
    }
}

public record UserContext(int UserId, string FirebaseUid, string Email, string? DisplayName, string[] Scopes);

