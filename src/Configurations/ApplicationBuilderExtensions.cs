namespace ShapeUp.Configurations;

using Features.AuditLogs.Infrastructure.Auditing;
using Features.Authorization.Infrastructure.Authorization;
using Features.PlatformFeatureFlags.Health;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

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

        // Availability SLI. /health/live never touches the DB (is the process itself up);
        // /health/ready additionally checks SQL Server connectivity (see ObservabilityExtensions).
        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
        app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

        // Per-feature health for clients that hide what is down. Always 200 (503 only if the process itself is unavailable),
        // anonymous (/health is a public path in AuthorizationMiddleware) and never exposes exceptions, connection strings or hosts.
        app.MapGet("/health", async (FeatureHealthService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.GetAsync(cancellationToken)));

        return app;
    }
}

