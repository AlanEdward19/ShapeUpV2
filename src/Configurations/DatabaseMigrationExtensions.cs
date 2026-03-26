namespace ShapeUp.Configurations;

using Microsoft.EntityFrameworkCore;
using ShapeUp.Features.AuditLogs.Shared.Data;
using ShapeUp.Features.Authorization.Shared.Data;
using ShapeUp.Features.GymManagement.Infrastructure.Data;

public static class DatabaseMigrationExtensions
{
    /// <summary>
    /// Applies pending EF Core migrations for all registered DbContexts on application startup.
    /// Creates the database if it does not exist.
    /// </summary>
    public static async Task ApplyMigrationsAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();

        await MigrateAsync<AuthorizationDbContext>(scope, app.Logger);
        await MigrateAsync<AuditLogsDbContext>(scope, app.Logger);
        await MigrateAsync<GymManagementDbContext>(scope, app.Logger);
    }

    private static async Task MigrateAsync<TContext>(AsyncServiceScope scope, ILogger logger)
        where TContext : DbContext
    {
        try
        {
            var context = scope.ServiceProvider.GetRequiredService<TContext>();
            var pending = await context.Database.GetPendingMigrationsAsync();
            var pendingList = pending.ToList();

            if (pendingList.Count == 0)
            {
                logger.LogInformation("[Migration] {Context}: up to date.", typeof(TContext).Name);
                return;
            }

            logger.LogInformation(
                "[Migration] {Context}: applying {Count} pending migration(s): {Migrations}",
                typeof(TContext).Name,
                pendingList.Count,
                string.Join(", ", pendingList));

            await context.Database.MigrateAsync();

            logger.LogInformation("[Migration] {Context}: completed successfully.", typeof(TContext).Name);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[Migration] {Context}: failed to apply migrations.", typeof(TContext).Name);
            throw;
        }
    }
}

