using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.EntityFrameworkCore;
using ShapeUp.Features.AuditLogs.Shared.Data;
using ShapeUp.Features.Authorization.Shared.Data;
using ShapeUp.Features.GymManagement.Infrastructure.Data;

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

    public GymManagementDbContext CreateGymManagementDbContext()
    {
        var options = new DbContextOptionsBuilder<GymManagementDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        return new GymManagementDbContext(options);
    }

    public async Task ResetDatabaseAsync(CancellationToken cancellationToken)
    {
        await using var authorizationContext = CreateAuthorizationDbContext();
        await authorizationContext.Database.EnsureDeletedAsync(cancellationToken);
        await authorizationContext.Database.EnsureCreatedAsync(cancellationToken);

        await using var auditLogsContext = CreateAuditLogsDbContext();
        await EnsureAuditLogSchemaAsync(auditLogsContext, cancellationToken);

        await using var gymContext = CreateGymManagementDbContext();
        await EnsureGymManagementSchemaAsync(gymContext, cancellationToken);
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

    private static async Task EnsureGymManagementSchemaAsync(GymManagementDbContext context, CancellationToken cancellationToken)
    {
        const string sql = """
                           IF OBJECT_ID(N'dbo.PlatformTiers', N'U') IS NULL
                           BEGIN
                               CREATE TABLE [dbo].[PlatformTiers] (
                                   [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                                   [Name] NVARCHAR(100) NOT NULL,
                                   [Description] NVARCHAR(MAX) NULL,
                                   [Price] DECIMAL(10,2) NOT NULL,
                                   [MaxClients] INT NULL,
                                   [MaxTrainers] INT NULL,
                                   [IsActive] BIT NOT NULL,
                                   [CreatedAt] DATETIME2 NOT NULL,
                                   [UpdatedAt] DATETIME2 NULL
                               );

                               CREATE UNIQUE INDEX [IX_PlatformTiers_Name] ON [dbo].[PlatformTiers]([Name]);
                           END

                           IF OBJECT_ID(N'dbo.UserPlatformRoles', N'U') IS NULL
                           BEGIN
                               CREATE TABLE [dbo].[UserPlatformRoles] (
                                   [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                                   [UserId] INT NOT NULL,
                                   [Role] INT NOT NULL,
                                   [PlatformTierId] INT NULL,
                                   [IsActive] BIT NOT NULL,
                                   [CreatedAt] DATETIME2 NOT NULL,
                                   CONSTRAINT [FK_UserPlatformRoles_PlatformTiers_PlatformTierId]
                                       FOREIGN KEY ([PlatformTierId]) REFERENCES [dbo].[PlatformTiers]([Id]) ON DELETE SET NULL
                               );

                               CREATE UNIQUE INDEX [IX_UserPlatformRoles_UserId_Role] ON [dbo].[UserPlatformRoles]([UserId], [Role]);
                           END

                           IF OBJECT_ID(N'dbo.Gyms', N'U') IS NULL
                           BEGIN
                               CREATE TABLE [dbo].[Gyms] (
                                   [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                                   [OwnerId] INT NOT NULL,
                                   [Name] NVARCHAR(200) NOT NULL,
                                   [Description] NVARCHAR(MAX) NULL,
                                   [Address] NVARCHAR(500) NULL,
                                   [PlatformTierId] INT NULL,
                                   [IsActive] BIT NOT NULL,
                                   [CreatedAt] DATETIME2 NOT NULL,
                                   [UpdatedAt] DATETIME2 NULL,
                                   CONSTRAINT [FK_Gyms_PlatformTiers_PlatformTierId]
                                       FOREIGN KEY ([PlatformTierId]) REFERENCES [dbo].[PlatformTiers]([Id]) ON DELETE SET NULL
                               );

                               CREATE INDEX [IX_Gyms_OwnerId] ON [dbo].[Gyms]([OwnerId]);
                           END

                           IF OBJECT_ID(N'dbo.GymPlans', N'U') IS NULL
                           BEGIN
                               CREATE TABLE [dbo].[GymPlans] (
                                   [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                                   [GymId] INT NOT NULL,
                                   [Name] NVARCHAR(100) NOT NULL,
                                   [Description] NVARCHAR(MAX) NULL,
                                   [Price] DECIMAL(10,2) NOT NULL,
                                   [DurationDays] INT NOT NULL,
                                   [IsActive] BIT NOT NULL,
                                   [CreatedAt] DATETIME2 NOT NULL,
                                   [UpdatedAt] DATETIME2 NULL,
                                   CONSTRAINT [FK_GymPlans_Gyms_GymId]
                                       FOREIGN KEY ([GymId]) REFERENCES [dbo].[Gyms]([Id]) ON DELETE CASCADE
                               );
                           END

                           IF OBJECT_ID(N'dbo.GymStaff', N'U') IS NULL
                           BEGIN
                               CREATE TABLE [dbo].[GymStaff] (
                                   [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                                   [GymId] INT NOT NULL,
                                   [UserId] INT NOT NULL,
                                   [Role] INT NOT NULL,
                                   [IsActive] BIT NOT NULL,
                                   [HiredAt] DATETIME2 NOT NULL,
                                   [CreatedAt] DATETIME2 NOT NULL,
                                   CONSTRAINT [FK_GymStaff_Gyms_GymId]
                                       FOREIGN KEY ([GymId]) REFERENCES [dbo].[Gyms]([Id]) ON DELETE CASCADE
                               );

                               CREATE UNIQUE INDEX [IX_GymStaff_GymId_UserId] ON [dbo].[GymStaff]([GymId], [UserId]);
                           END

                           IF OBJECT_ID(N'dbo.GymClients', N'U') IS NULL
                           BEGIN
                               CREATE TABLE [dbo].[GymClients] (
                                   [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                                   [GymId] INT NOT NULL,
                                   [UserId] INT NOT NULL,
                                   [GymPlanId] INT NOT NULL,
                                   [TrainerId] INT NULL,
                                   [IsActive] BIT NOT NULL,
                                   [EnrolledAt] DATETIME2 NOT NULL,
                                   [CreatedAt] DATETIME2 NOT NULL,
                                   CONSTRAINT [FK_GymClients_Gyms_GymId]
                                       FOREIGN KEY ([GymId]) REFERENCES [dbo].[Gyms]([Id]) ON DELETE CASCADE,
                                   CONSTRAINT [FK_GymClients_GymPlans_GymPlanId]
                                       FOREIGN KEY ([GymPlanId]) REFERENCES [dbo].[GymPlans]([Id]) ON DELETE NO ACTION,
                                   CONSTRAINT [FK_GymClients_GymStaff_TrainerId]
                                       FOREIGN KEY ([TrainerId]) REFERENCES [dbo].[GymStaff]([Id]) ON DELETE NO ACTION
                               );

                               CREATE UNIQUE INDEX [IX_GymClients_GymId_UserId] ON [dbo].[GymClients]([GymId], [UserId]);
                           END

                           IF OBJECT_ID(N'dbo.TrainerPlans', N'U') IS NULL
                           BEGIN
                               CREATE TABLE [dbo].[TrainerPlans] (
                                   [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                                   [TrainerId] INT NOT NULL,
                                   [Name] NVARCHAR(100) NOT NULL,
                                   [Description] NVARCHAR(MAX) NULL,
                                   [Price] DECIMAL(10,2) NOT NULL,
                                   [DurationDays] INT NOT NULL,
                                   [IsActive] BIT NOT NULL,
                                   [CreatedAt] DATETIME2 NOT NULL,
                                   [UpdatedAt] DATETIME2 NULL
                               );

                               CREATE INDEX [IX_TrainerPlans_TrainerId] ON [dbo].[TrainerPlans]([TrainerId]);
                           END

                           IF OBJECT_ID(N'dbo.TrainerClients', N'U') IS NULL
                           BEGIN
                               CREATE TABLE [dbo].[TrainerClients] (
                                   [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                                   [TrainerId] INT NOT NULL,
                                   [ClientId] INT NOT NULL,
                                   [TrainerPlanId] INT NOT NULL,
                                   [IsActive] BIT NOT NULL,
                                   [EnrolledAt] DATETIME2 NOT NULL,
                                   [CreatedAt] DATETIME2 NOT NULL,
                                   CONSTRAINT [FK_TrainerClients_TrainerPlans_TrainerPlanId]
                                       FOREIGN KEY ([TrainerPlanId]) REFERENCES [dbo].[TrainerPlans]([Id]) ON DELETE NO ACTION
                               );

                               CREATE UNIQUE INDEX [IX_TrainerClients_TrainerId_ClientId] ON [dbo].[TrainerClients]([TrainerId], [ClientId]);
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


