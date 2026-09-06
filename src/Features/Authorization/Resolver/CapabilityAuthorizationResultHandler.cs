namespace ShapeUp.Features.Authorization.Resolver;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;

/// <summary>
/// SPEC_DEVIATION (discovered while implementing T9, native-authorization-model): this app never
/// registers an ASP.NET Core authentication scheme -- Firebase tokens are verified entirely by the
/// app's own AuthorizationMiddleware, which populates HttpContext.Items["User"], never
/// HttpContext.User. Because of that, the framework's default IAuthorizationMiddlewareResultHandler
/// always treats a CapabilityRequirement failure as "not authenticated" (Challenged) rather than
/// "authenticated but denied" (Forbidden), and crashes with a 500 trying to invoke a challenge
/// scheme that was never configured -- instead of the 403 that AUTHZ-04/AUTHZ-05 require. This is
/// a gap in T7's DI wiring (AddCapabilityResolverDependencies calls AddAuthorization() but never
/// AddAuthentication()), not something specific to GymStaffController; it blocks every controller
/// migrated to [Authorize(Policy = "capability:...")] (T8, T10-T24), so it is fixed once here
/// rather than duplicated per migration task. Every capability policy only ever needs a 403 on
/// denial -- there is no login/challenge redirect in this API -- so any authorization failure maps
/// straight to Forbid.
/// </summary>
public class CapabilityAuthorizationResultHandler : IAuthorizationMiddlewareResultHandler
{
    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Succeeded)
        {
            await next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
    }
}
