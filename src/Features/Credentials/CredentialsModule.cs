namespace ShapeUp.Features.Credentials;

using Microsoft.EntityFrameworkCore;
using Shared.Abstractions;
using Shared.Data;
using Infrastructure.Repositories;

public static class CredentialsModule
{
    public static IServiceCollection AddCredentialsServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")!;
        services.AddDbContext<CredentialsDbContext>(options => options.UseSqlServer(connectionString));

        services.AddScoped<IProfessionalCredentialRepository, ProfessionalCredentialRepository>();

        return services;
    }
}
