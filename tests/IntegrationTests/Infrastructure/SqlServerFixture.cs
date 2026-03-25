using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.EntityFrameworkCore;
using ShapeUp.Features.AuditLogs.Shared.Data;
using ShapeUp.Features.Authorization.Shared.Data;

namespace IntegrationTests.Infrastructure;

public sealed class SqlServerFixture : IAsyncLifetime
{
    private const string DatabaseName = "ShapeUpIntegrationTests";
    private const string SaPassword = "Your_strong_password_123!";
    private static readonly SemaphoreSlim InitLock = new(1, 1);
    private static IContainer? _container;
    private static string? _connectionString;

    public string ConnectionString => _connectionString
        ?? throw new InvalidOperationException("SQL Server container is not initialized.");

    public async Task InitializeAsync()
    {
        await InitLock.WaitAsync();
        try
        {
            if (_container != null)
                return;

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
        finally
        {
            InitLock.Release();
        }

        await ResetDatabaseAsync(CancellationToken.None);
    }

    public Task DisposeAsync()
    {
        // Container cleanup is handled by Testcontainers resource reaper.
        return Task.CompletedTask;
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

    public async Task ResetDatabaseAsync(CancellationToken cancellationToken)
    {
        await using var authorizationContext = CreateAuthorizationDbContext();
        await authorizationContext.Database.EnsureDeletedAsync(cancellationToken);
        await authorizationContext.Database.EnsureCreatedAsync(cancellationToken);

        await using var auditLogsContext = CreateAuditLogsDbContext();
        await EnsureAuditLogSchemaAsync(auditLogsContext, cancellationToken);
    }

    private static async Task EnsureAuditLogSchemaAsync(AuditLogsDbContext context, CancellationToken cancellationToken)
    {
        const string sql = """
                           IF OBJECT_ID(N'dbo.AuditLogEntries', N'U') IS NULL
                           BEGIN
                               CREATE TABLE [dbo].[AuditLogEntries] (
                                   [Id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                                   [OccurredAtUtc] DATETIME2 NOT NULL,
                                   [UserEmail] NVARCHAR(320) NULL,
                                   [HttpMethod] NVARCHAR(10) NOT NULL,
                                   [Endpoint] NVARCHAR(512) NOT NULL,
                                   [QueryParametersJson] NVARCHAR(4000) NULL,
                                   [RequestBodyJson] NVARCHAR(4000) NULL,
                                   [StatusCode] INT NOT NULL,
                                   [DurationMs] BIGINT NOT NULL,
                                   [TraceId] NVARCHAR(128) NULL,
                                   [IpAddress] NVARCHAR(64) NULL,
                                   [UserAgent] NVARCHAR(512) NULL
                               );

                               CREATE INDEX [IX_AuditLogEntries_OccurredAtUtc] ON [dbo].[AuditLogEntries] ([OccurredAtUtc]);
                               CREATE INDEX [IX_AuditLogEntries_UserEmail] ON [dbo].[AuditLogEntries] ([UserEmail]);
                               CREATE INDEX [IX_AuditLogEntries_Endpoint] ON [dbo].[AuditLogEntries] ([Endpoint]);
                           END
                           """;

        await context.Database.ExecuteSqlRawAsync(sql, cancellationToken);
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


