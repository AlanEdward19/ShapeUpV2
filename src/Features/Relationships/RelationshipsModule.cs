namespace ShapeUp.Features.Relationships;

using Microsoft.EntityFrameworkCore;
using Shared.Abstractions;
using Shared.Data;
using Infrastructure.Repositories;

public static class RelationshipsModule
{
    public static IServiceCollection AddRelationshipsServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")!;
        services.AddDbContext<RelationshipsDbContext>(options => options.UseSqlServer(connectionString));

        services.AddScoped<IProfessionalClientRelationshipRepository, ProfessionalClientRelationshipRepository>();

        return services;
    }
}
