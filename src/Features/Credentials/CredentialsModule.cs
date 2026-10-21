namespace ShapeUp.Features.Credentials;

using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Shared.Abstractions;
using Shared.Data;
using Shared.Lifecycle;
using Shared.Verification;
using Infrastructure.Repositories;
using Infrastructure.Verification;
using Infrastructure.Notifications;
using EndCredential;
using ExpireCredentials;
using GetCredentialsUnderReview;
using GetVerifiedProfessionals;
using GetMyCredentials;
using ReviewCredential;
using SubmitCredential;
using ResubmitCredential;
using Shared.Notifications;

public static class CredentialsModule
{
    public static IServiceCollection AddCredentialsServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")!;
        services.AddDbContext<CredentialsDbContext>(options => options.UseSqlServer(connectionString));

        services.AddScoped<IProfessionalCredentialRepository, ProfessionalCredentialRepository>();

        services.AddScoped<ICredentialNotifier, EmailCredentialNotifier>();

        // No council publishes a lookup API: every CREF/CRN submission goes to the admin review queue.
        services.AddScoped<ICredentialVerifier, ManualReviewCredentialVerifier>();
        services.AddScoped<IProfessionalRoleGranter, ProfessionalRoleGranter>();
        services.AddScoped<ICredentialLifecycle, CredentialLifecycle>();

        services.AddScoped<IValidator<SubmitCredentialCommand>, SubmitCredentialValidator>();
        services.AddScoped<IValidator<RejectCredentialCommand>, RejectCredentialValidator>();
        services.AddScoped<IValidator<ApproveCredentialCommand>, ApproveCredentialValidator>();
        services.AddScoped<IValidator<EndCredentialCommand>, EndCredentialValidator>();
        services.AddScoped<IValidator<RequestChangesCommand>, RequestChangesValidator>();
        services.AddScoped<IValidator<ResubmitCredentialCommand>, ResubmitCredentialValidator>();
        services.AddScoped<ResubmitCredentialHandler>();
        services.AddScoped<CredentialRequesterEnricher>();
        services.AddScoped<EndCredentialHandler>();
        services.AddScoped<SubmitCredentialHandler>();
        services.AddScoped<GetMyCredentialsHandler>();
        services.AddScoped<GetCredentialsUnderReviewHandler>();
        services.AddScoped<IValidator<GetVerifiedProfessionalsQuery>, GetVerifiedProfessionalsValidator>();
        services.AddScoped<GetVerifiedProfessionalsHandler>();
        services.AddScoped<ReviewCredentialHandler>();
        services.AddScoped<ExpireCredentialsHandler>();
        services.AddHostedService<ExpireCredentialsHostedService>();

        services.AddScoped<ShapeUp.Shared.Privacy.IUserDataEraser, ShapeUp.Features.Credentials.Privacy.CredentialsUserDataEraser>();

        return services;
    }
}
