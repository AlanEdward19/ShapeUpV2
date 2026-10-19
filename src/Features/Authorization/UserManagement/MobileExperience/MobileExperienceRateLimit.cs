using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using ShapeUp.Features.Authorization.Shared.Extensions;

namespace ShapeUp.Features.Authorization.UserManagement.MobileExperience;

public static class MobileExperienceRateLimit
{
    public const string Policy = "mobile-experience";
    public static IServiceCollection AddMobileExperienceRateLimit(this IServiceCollection services) =>
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(Policy, context => RateLimitPartition.GetFixedWindowLimiter(
                context.GetUserContext()?.UserId.ToString() ?? "anonymous",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0
                }));
        });
}
