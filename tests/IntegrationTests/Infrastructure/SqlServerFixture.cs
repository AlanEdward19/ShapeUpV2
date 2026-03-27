using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.EntityFrameworkCore;
using Mongo2Go;
using ShapeUp.Features.AuditLogs.Shared.Data;
using ShapeUp.Features.Authorization.Shared.Data;
using ShapeUp.Features.GymManagement.Infrastructure.Data;
using ShapeUp.Features.Training.Infrastructure.Data;

namespace IntegrationTests.Infrastructure;

public sealed class SqlServerFixture : IAsyncLifetime
{
    private const string DatabaseName = "ShapeUpIntegrationTests";
    private const string SaPassword = "Your_strong_password_123!";
    private static readonly SemaphoreSlim InitLock = new(1, 1);
    private static IContainer? _container;
    private static MongoDbRunner? _mongoRunner;
    private static string? _connectionString;
    private static bool _databasePrepared;
    private static int _activeFixtureCount;
    private bool _initialized;

    public string ConnectionString => _connectionString
        ?? throw new InvalidOperationException("SQL Server container is not initialized.");

    public string MongoConnectionString => _mongoRunner?.ConnectionString
        ?? throw new InvalidOperationException("MongoDB runner is not initialized.");

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
            if (_container == null)
            {
                _container = new ContainerBuilder()
                    .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
                    .WithEnvironment("ACCEPT_EULA", "Y")
                    .WithEnvironment("MSSQL_SA_PASSWORD", SaPassword)
                    .WithPortBinding(1433, assignRandomHostPort: true)
                    .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(1433))
                    .Build();

                await _container.StartAsync();

                var host = _container.Hostname;
                var port = _container.GetMappedPublicPort(1433);
                var masterBuilder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder
                {
                    DataSource = $"{host},{port}",
                    UserID = "sa",
                    Password = SaPassword,
                    InitialCatalog = "master",
                    Encrypt = false,
                    TrustServerCertificate = true,
                    ConnectTimeout = 5
                };

                var masterConnectionString = masterBuilder.ConnectionString;
                await WaitForSqlServerReadyAsync(masterConnectionString, CancellationToken.None);
                await EnsureDatabaseExistsAsync(masterConnectionString, CancellationToken.None);

                masterBuilder.InitialCatalog = DatabaseName;
                _connectionString = masterBuilder.ConnectionString;
            }

            _mongoRunner ??= MongoDbRunner.Start(singleNodeReplSet: true);

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

            _mongoRunner?.Dispose();
            _mongoRunner = null;

            if (_container is not null)
            {
                await _container.DisposeAsync();
                _container = null;
            }

            _connectionString = null;
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

        // Baseline scopes used by endpoint authorization tests.
        var baselineScopes = new[]
        {
            ("audit:logs:read", "audit", "logs", "read"),
            ("groups:management:create", "groups", "management", "create"),
            ("groups:management:delete", "groups", "management", "delete"),
            ("scopes:management:create", "scopes", "management", "create")
        };

        foreach (var (name, domain, subdomain, action) in baselineScopes)
        {
            if (await authContext.Scopes.AnyAsync(s => s.Name == name, cancellationToken))
                continue;

            authContext.Scopes.Add(new ShapeUp.Features.Authorization.Shared.Entities.Scope
            {
                Name = name,
                Domain = domain,
                Subdomain = subdomain,
                Action = action,
                Description = "integration baseline"
            });
        }

        await authContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task WaitForSqlServerReadyAsync(string connectionString, CancellationToken cancellationToken)
    {
        const int maxAttempts = 30;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
                await connection.OpenAsync(cancellationToken);

                await using var command = connection.CreateCommand();
                command.CommandText = "SELECT 1";
                await command.ExecuteScalarAsync(cancellationToken);
                return;
            }
            catch when (attempt < maxAttempts)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }

        throw new InvalidOperationException("SQL Server container started but did not become ready in time.");
    }

    private static async Task EnsureDatabaseExistsAsync(string masterConnectionString, CancellationToken cancellationToken)
    {
        await using var connection = new Microsoft.Data.SqlClient.SqlConnection(masterConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = $"IF DB_ID(N'{DatabaseName}') IS NULL CREATE DATABASE [{DatabaseName}]";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}


