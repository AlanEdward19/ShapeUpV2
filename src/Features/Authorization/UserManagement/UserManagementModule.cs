using ShapeUp.Features.Authorization.Infrastructure.Repositories;
using ShapeUp.Features.Authorization.ProfessionalCapabilities;
using ShapeUp.Features.Authorization.Shared.Abstractions;
using ShapeUp.Features.Authorization.UserManagement.GetUser;
using ShapeUp.Features.Authorization.UserManagement.MobileExperience;
using FluentValidation;
using ShapeUp.Features.Authorization.UserManagement.RevokeCurrentToken;

namespace ShapeUp.Features.Authorization.UserManagement;

public static class UserManagementModule
{
    public static void AddUserManagementServices(this IServiceCollection services)
    {
        services
            .AddAuthorizationDependencies();
    }

    private static void AddAuthorizationDependencies(this IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();

        services.AddScoped<GetUserHandler>();
        services.AddScoped<ShapeUp.Features.Authorization.UserManagement.DeleteAccount.DeleteAccountHandler>();
        services.AddScoped<RegisterMobileExperienceHandler>();
        services.AddScoped<IValidator<RegisterMobileExperienceCommand>, RegisterMobileExperienceValidator>();
        services.AddMobileExperienceRateLimit();
        services.AddScoped<RevokeCurrentTokenHandler>();
        services.AddScoped<IProfessionalCapabilityService, ProfessionalCapabilityService>();
        services.AddScoped<GetProfessionalCapabilitiesHandler>();
    }
}