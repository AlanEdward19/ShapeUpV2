namespace ShapeUp.Features.Authorization.Resolver;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Infrastructure.Authorization;

/// <summary>
/// Native ASP.NET Core policy handler for capability-based authorization (AD-001). Reads the
/// UserContext populated by the existing AuthorizationMiddleware, builds an AuthorizationContext
/// from the current request's route values, and asks ICapabilityResolver to decide.
///
/// Deny-by-default (AUTHZ-05): any exception from the resolver is audited as a failed decision
/// and rethrown -- it never calls Succeed, and the rethrow lets the framework's default
/// exception handling turn it into a 500, matching spec.md AUTHZ-05.
/// </summary>
public class CapabilityAuthorizationHandler(
    IHttpContextAccessor httpContextAccessor,
    ICapabilityResolver resolver,
    IAuthorizationAuditWriter auditWriter) : AuthorizationHandler<CapabilityRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, CapabilityRequirement requirement)
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null)
            return;

        if (!httpContext.Items.TryGetValue("User", out var userObj) || userObj is not UserContext user)
            return;

        var cancellationToken = httpContext.RequestAborted;
        var authContext = BuildAuthorizationContext(httpContext, requirement.Capability);

        CapabilityResult result;
        try
        {
            result = await resolver.ResolveAsync(user.UserId, requirement.Capability, authContext, cancellationToken);
        }
        catch
        {
            await auditWriter.RecordAsync(user.UserId, requirement.Capability, allowed: false, reason: "internal_error", authContext, cancellationToken);
            throw;
        }

        await auditWriter.RecordAsync(user.UserId, requirement.Capability, result.IsAllowed, result.DenyReason, authContext, cancellationToken);

        if (result.IsAllowed)
            context.Succeed(requirement);
        else
            context.Fail(new AuthorizationFailureReason(this, result.DenyReason ?? "Capability denied."));
    }

    /// <summary>
    /// Capability names prefixed "platform." are a naming convention (AD-003/AD-006), not
    /// per-capability config: they require the acting user to hold PlatformRoleType.Admin,
    /// regardless of any route value. There is no owner/gym/relationship for a platform-wide
    /// action (e.g. curating the shared exercise catalog), so this check is exclusive -- it never
    /// falls back to self-access or membership.
    /// </summary>
    private const string PlatformAdminCapabilityPrefix = "platform.";

    private static AuthorizationContext BuildAuthorizationContext(HttpContext httpContext, string capability)
    {
        if (capability.StartsWith(PlatformAdminCapabilityPrefix, StringComparison.Ordinal))
            return new AuthorizationContext(RequiresPlatformAdmin: true);

        var gymId = TryGetRouteInt(httpContext, "gymId");
        var targetUserId = TryGetRouteInt(httpContext, "userId")
                            ?? TryGetRouteInt(httpContext, "clientUserId")
                            ?? TryGetRouteInt(httpContext, "staffId")
                            ?? TryGetRouteInt(httpContext, "trainerId");

        return new AuthorizationContext(GymId: gymId, TargetUserId: targetUserId);
    }

    private static int? TryGetRouteInt(HttpContext httpContext, string key)
    {
        if (!httpContext.Request.RouteValues.TryGetValue(key, out var value) || value is null)
            return null;

        return int.TryParse(value.ToString(), out var parsed) ? parsed : null;
    }
}
