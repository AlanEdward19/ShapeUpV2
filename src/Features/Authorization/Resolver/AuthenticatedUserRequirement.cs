using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using ShapeUp.Features.Authorization.Infrastructure.Authorization;

namespace ShapeUp.Features.Authorization.Resolver;

/// <summary>
/// Succeeds only when AuthorizationMiddleware has stored the signed-in UserContext.
/// Used by routes that any authenticated caller may use (directory, own inbox).
/// </summary>
public sealed class AuthenticatedUserRequirement : IAuthorizationRequirement;

public class AuthenticatedUserHandler(IHttpContextAccessor httpContextAccessor)
    : AuthorizationHandler<AuthenticatedUserRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, AuthenticatedUserRequirement requirement)
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is not null
            && httpContext.Items.TryGetValue("User", out var user)
            && user is UserContext)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
