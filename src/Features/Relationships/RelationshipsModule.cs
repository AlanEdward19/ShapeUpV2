namespace ShapeUp.Features.Relationships;

using Microsoft.EntityFrameworkCore;
using Shared.Data;

public static class RelationshipsModule
{
    public static IServiceCollection AddRelationshipsServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")!;
        services.AddDbContext<RelationshipsDbContext>(options => options.UseSqlServer(connectionString));

        return services;
    }
}
