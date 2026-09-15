using Microsoft.EntityFrameworkCore;
using ShapeUp.Features.AuditLogs.Shared.Data;
using ShapeUp.Features.Authorization.Shared.Data;
using ShapeUp.Features.Gamification.Infrastructure.Data;
using ShapeUp.Features.GymManagement.Infrastructure.Data;
using ShapeUp.Features.Nutrition.Infrastructure.Data;
using ShapeUp.Features.PlatformFeatureFlags.Infrastructure.Data;
using ShapeUp.Features.Relationships.Shared.Data;
using ShapeUp.Features.Training.Infrastructure.Data;

namespace IntegrationTests.Infrastructure;

public sealed class SqlServerFixture : IAsyncLifetime
{
    private static readonly SemaphoreSlim InitLock = new(1, 1);
    private static bool _databasePrepared;
    private static int _activeFixtureCount;
    private bool _initialized;

    public string ConnectionString => IntegrationTestContainers.SqlConnectionString;

    public string MongoConnectionString => IntegrationTestContainers.MongoConnectionString;

    public async Task InitializeAsync()
    {
        if (!_initialized)
        {
            Interlocked.Increment(ref _activeFixtureCount);
            _initialized = true;
        }

        await InitLock.WaitAsync();
        try
        {
            await IntegrationTestContainers.AcquireSqlAsync(CancellationToken.None);
            await IntegrationTestContainers.AcquireMongoAsync(CancellationToken.None);

            if (!_databasePrepared)
            {
                await PrepareDatabaseAsync(CancellationToken.None);
                _databasePrepared = true;
            }
        }
        finally
        {
            InitLock.Release();
        }
    }

    public async Task DisposeAsync()
    {
        if (!_initialized)
            return;

        await InitLock.WaitAsync();
        try
        {
            if (!_initialized)
                return;

            _initialized = false;

            if (Interlocked.Decrement(ref _activeFixtureCount) != 0)
                return;

            await IntegrationTestContainers.ReleaseSqlAsync();
            await IntegrationTestContainers.ReleaseMongoAsync();
            _databasePrepared = false;
        }
        finally
        {
            InitLock.Release();
        }
    }

    public AuthorizationDbContext CreateAuthorizationDbContext()
    {
        var options = new DbContextOptionsBuilder<AuthorizationDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        return new AuthorizationDbContext(options);
    }

    public AuditLogsDbContext CreateAuditLogsDbContext()
    {
        var options = new DbContextOptionsBuilder<AuditLogsDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        return new AuditLogsDbContext(options);
    }

    public GymManagementDbContext CreateGymManagementDbContext()
    {
        var options = new DbContextOptionsBuilder<GymManagementDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        return new GymManagementDbContext(options);
    }

    public TrainingDbContext CreateTrainingDbContext()
    {
        var options = new DbContextOptionsBuilder<TrainingDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        return new TrainingDbContext(options);
    }

    public RelationshipsDbContext CreateRelationshipsDbContext()
    {
        var options = new DbContextOptionsBuilder<RelationshipsDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        return new RelationshipsDbContext(options);
    }

    public GamificationDbContext CreateGamificationDbContext()
    {
        var options = new DbContextOptionsBuilder<GamificationDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        return new GamificationDbContext(options);
    }

    public NutritionDbContext CreateNutritionDbContext()
    {
        var options = new DbContextOptionsBuilder<NutritionDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        return new NutritionDbContext(options);
    }

    public PlatformFeatureFlagsDbContext CreatePlatformFeatureFlagsDbContext()
    {
        var options = new DbContextOptionsBuilder<PlatformFeatureFlagsDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        return new PlatformFeatureFlagsDbContext(options);
    }

    public async Task ResetDatabaseAsync(CancellationToken cancellationToken)
    {
        // Database is initialized only once in InitializeAsync
        // Tests within a Collection maintain state between executions
        // This avoids repeated migration conflicts
        await Task.CompletedTask;
    }

    private async Task PrepareDatabaseAsync(CancellationToken cancellationToken)
    {
        await using var authContext = CreateAuthorizationDbContext();
        await authContext.Database.MigrateAsync(cancellationToken);

        await using var auditContext = CreateAuditLogsDbContext();
        await auditContext.Database.MigrateAsync(cancellationToken);

        await using var gymContext = CreateGymManagementDbContext();
        await gymContext.Database.MigrateAsync(cancellationToken);

        await using var trainingContext = CreateTrainingDbContext();
        await trainingContext.Database.MigrateAsync(cancellationToken);

        await using var relationshipsContext = CreateRelationshipsDbContext();
        await relationshipsContext.Database.MigrateAsync(cancellationToken);

        await using var gamificationContext = CreateGamificationDbContext();
        await gamificationContext.Database.MigrateAsync(cancellationToken);

        await using var nutritionContext = CreateNutritionDbContext();
        await nutritionContext.Database.MigrateAsync(cancellationToken);

        await using var featureFlagsContext = CreatePlatformFeatureFlagsDbContext();
        await featureFlagsContext.Database.MigrateAsync(cancellationToken);
    }
}


