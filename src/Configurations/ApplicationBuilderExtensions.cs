namespace ShapeUp.Configurations;

using Features.AuditLogs.Infrastructure.Auditing;
using Features.Authorization.Infrastructure.Authorization;

public static class ApplicationBuilderExtensions
{
    public static WebApplication UseProjectPipeline(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();
        app.UseMiddleware<AuditLoggingMiddleware>();
        app.UseMiddleware<AuthorizationMiddleware>();
        app.UseAuthorization();
        app.MapControllers();

        return app;
    }
}

