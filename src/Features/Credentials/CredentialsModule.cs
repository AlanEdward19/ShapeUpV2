namespace ShapeUp.Features.Credentials;

using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Shared.Abstractions;
using Shared.Data;
using Shared.Lifecycle;
using Shared.Verification;
using Infrastructure.Repositories;
using Infrastructure.Verification;
using EndCredential;
using ExpireCredentials;
using GetCredentialsUnderReview;
using GetMyCredentials;
using ReviewCredential;
using SubmitCredential;

public static class CredentialsModule
{
    public static IServiceCollection AddCredentialsServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")!;
        services.AddDbContext<CredentialsDbContext>(options => options.UseSqlServer(connectionString));

        services.AddScoped<IProfessionalCredentialRepository, ProfessionalCredentialRepository>();

        // Swap or add verifiers here when a council offers an automatic check.
        services.AddScoped<ICredentialVerifier, ManualReviewCredentialVerifier>();
        services.AddScoped<IProfessionalRoleGranter, ProfessionalRoleGranter>();
        services.AddScoped<ICredentialLifecycle, CredentialLifecycle>();

        services.AddScoped<IValidator<SubmitCredentialCommand>, SubmitCredentialValidator>();
        services.AddScoped<IValidator<RejectCredentialCommand>, RejectCredentialValidator>();
        services.AddScoped<IValidator<ApproveCredentialCommand>, ApproveCredentialValidator>();
        services.AddScoped<IValidator<EndCredentialCommand>, EndCredentialValidator>();
        services.AddScoped<CredentialRequesterEnricher>();
        services.AddScoped<EndCredentialHandler>();
        services.AddScoped<SubmitCredentialHandler>();
        services.AddScoped<GetMyCredentialsHandler>();
        services.AddScoped<GetCredentialsUnderReviewHandler>();
        services.AddScoped<ReviewCredentialHandler>();
        services.AddScoped<ExpireCredentialsHandler>();
        services.AddHostedService<ExpireCredentialsHostedService>();

        return services;
    }
}
