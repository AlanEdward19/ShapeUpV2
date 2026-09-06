namespace ShapeUp.Features.Credentials;

using Microsoft.EntityFrameworkCore;
using Shared.Data;

public static class CredentialsModule
{
    public static IServiceCollection AddCredentialsServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")!;
        services.AddDbContext<CredentialsDbContext>(options => options.UseSqlServer(connectionString));

        return services;
    }
}
