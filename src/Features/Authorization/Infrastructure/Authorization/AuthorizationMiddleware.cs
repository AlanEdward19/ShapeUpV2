namespace ShapeUp.Features.Authorization.Infrastructure.Authorization;

using Shared.Abstractions;
using ShapeUp.Features.GymManagement.Shared.Abstractions;
using GymPlatformRoleType = ShapeUp.Features.GymManagement.Shared.Entities.PlatformRoleType;
using GymUserPlatformRole = ShapeUp.Features.GymManagement.Shared.Entities.UserPlatformRole;

/// <summary>
/// Middleware to validate Firebase tokens and provision users.
/// Extracts user context and adds it to HttpContext.Items for use in handlers.
/// </summary>
public class AuthorizationMiddleware(ILogger<AuthorizationMiddleware> logger) : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var cancellationToken = context.RequestAborted;

        if (IsPublicEndpoint(context.Request.Path))
        {
            await next(context);
            return;
        }

        var authHeader = context.Request.Headers.Authorization.ToString();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Missing or invalid authorization header" }, cancellationToken);
            return;
        }

        var token = authHeader.Substring("Bearer ".Length).Trim();

        try
        {
            var firebaseService = context.RequestServices.GetRequiredService<IFirebaseService>();
            var userRepository = context.RequestServices.GetRequiredService<IUserRepository>();
            var scopeRepository = context.RequestServices.GetRequiredService<IScopeRepository>();
            var userPlatformRoleRepository = context.RequestServices.GetRequiredService<IUserPlatformRoleRepository>();

            var tokenResult = await firebaseService.VerifyTokenAsync(token, cancellationToken);
            if (tokenResult.IsFailure)
            {
                var error = tokenResult.Error!;
                context.Response.StatusCode = error.StatusCode;
                await context.Response.WriteAsJsonAsync(new { error.Code, error.Message }, cancellationToken);
                return;
            }

            var firebaseTokenData = tokenResult.Value!;

            var user = await userRepository.GetByFirebaseUidAsync(firebaseTokenData.Uid, cancellationToken);
            if (user == null)
            {
                user = new Shared.Entities.User
                {
                    FirebaseUid = firebaseTokenData.Uid,
                    Email = firebaseTokenData.Email,
                    DisplayName = firebaseTokenData.DisplayName,
                    IsActive = true
                };
                await userRepository.AddAsync(user, cancellationToken);

                var independentClientRole = await userPlatformRoleRepository.GetByUserIdAndRoleAsync(
                    user.Id,
                    GymPlatformRoleType.IndependentClient,
                    cancellationToken);
                if (independentClientRole is null)
                {
                    await userPlatformRoleRepository.AddAsync(new GymUserPlatformRole
                    {
                        UserId = user.Id,
                        Role = GymPlatformRoleType.IndependentClient
                    }, cancellationToken);
                }

                var existingClaimsResult = await firebaseService.GetCustomClaimsAsync(user.FirebaseUid, cancellationToken);
                if (existingClaimsResult.IsFailure)
                {
                    var error = existingClaimsResult.Error!;
                    context.Response.StatusCode = error.StatusCode;
                    await context.Response.WriteAsJsonAsync(new { error.Code, error.Message }, cancellationToken);
                    return;
                }

                var claims = existingClaimsResult.Value!;
                claims["userId"] = user.Id;

                var setClaimsResult = await firebaseService.SetCustomClaimsAsync(user.FirebaseUid, claims, cancellationToken);
                if (setClaimsResult.IsFailure)
                {
                    var error = setClaimsResult.Error!;
                    context.Response.StatusCode = error.StatusCode;
                    await context.Response.WriteAsJsonAsync(new { error.Code, error.Message }, cancellationToken);
                    return;
                }
            }

            var scopes = await scopeRepository.GetUserScopesAsync(user.Id, cancellationToken);
            var scopeNames = scopes.Select(s => s.Name).ToArray();

            var userContext = new UserContext(
                user.Id,
                user.FirebaseUid,
                user.Email,
                user.DisplayName ?? firebaseTokenData.DisplayName,
                scopeNames);
            context.Items["User"] = userContext;
            context.Items["UserId"] = user.Id;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while authorizing request.");
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new { error = "internal_error", message = "An unexpected authorization error occurred." }, cancellationToken);
            return;
        }

        await next(context);
    }

    private static bool IsPublicEndpoint(PathString path)
    {
        var publicPaths = new[]
        {
            "/health",
            "/openapi",
            "/swagger"
        };

        return publicPaths.Any(p => path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase));
    }
}
