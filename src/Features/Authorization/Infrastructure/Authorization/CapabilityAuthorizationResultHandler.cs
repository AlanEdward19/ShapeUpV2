namespace ShapeUp.Features.Authorization.Infrastructure.Authorization;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

/// <summary>
/// SPEC_DEVIATION (found while implementing T8): this app never calls AddAuthentication() --
/// Firebase tokens are verified manually by AuthorizationMiddleware and stashed in
/// HttpContext.Items, not through an IAuthenticationHandler/ClaimsPrincipal. The framework's
/// default IAuthorizationMiddlewareResultHandler reacts to a denied [Authorize] policy by calling
/// ChallengeAsync/ForbidAsync, which both require a registered authentication scheme -- with none
/// registered, every denied [Authorize(Policy = "capability:...")] request threw
/// InvalidOperationException("No authenticationScheme was specified...") and surfaced as a 500,
/// not the expected 403. This handler replaces the default: any non-succeeded policy result is
/// written as a plain 403, matching the behavior RequireScopesAttribute already had (see
/// RequireScopesAttribute.OnAuthorizationAsync, which sets a 403 ObjectResult directly and never
/// goes through ASP.NET Core's Challenge/Forbid pipeline). This is required for every controller
/// migrated to capability policies (T8-T24), not specific to GymsController -- T8 is simply the
/// first to add a real end-to-end HTTP deny test that exercises it.
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
        await context.Response.WriteAsJsonAsync(new { Code = "forbidden", Message = "Capability denied." });
    }
}
