namespace IntegrationTests.Infrastructure;

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Docker.DotNet.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using RabbitMQ.Client;

public static class IntegrationTestContainers
{
    private const string MongoReplicaSetName = "rs0";
    private const string SqlDatabaseName = "ShapeUpIntegrationTests";
    private const string SqlSaPassword = "Your_strong_password_123!";
    private const ushort MongoPort = 27017;
    private const ushort RabbitPort = 5672;
    private const ushort SqlPort = 1433;

    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static IContainer? _mongo;
    private static IContainer? _rabbit;
    private static IContainer? _sql;
    private static int _mongoUsers;
    private static int _rabbitUsers;
    private static int _sqlUsers;
    private static string? _mongoConnectionString;
    private static string? _sqlConnectionString;
    private static string? _rabbitHost;
    private static ushort _rabbitMappedPort;

    static IntegrationTestContainers()
    {
        // Ryuk keep-alive over the Docker named pipe flakes on Windows ("Unexpected end of stream",
        // "DockerContainer not found") when Messaging + SQL collections start containers in parallel.
        Environment.SetEnvironmentVariable("TESTCONTAINERS_RYUK_DISABLED", "true");
        TestcontainersSettings.ResourceReaperEnabled = false;
    }

    public static string SqlConnectionString =>
        _sqlConnectionString ?? throw new InvalidOperationException("SQL Server Testcontainer is not started.");

    public static string MongoConnectionString =>
        _mongoConnectionString ?? throw new InvalidOperationException("MongoDB Testcontainer is not started.");

    public static string RabbitHost =>
        _rabbitHost ?? throw new InvalidOperationException("RabbitMQ Testcontainer is not started.");

    public static ushort RabbitMappedPort =>
        _rabbitMappedPort == 0
            ? throw new InvalidOperationException("RabbitMQ Testcontainer is not started.")
            : _rabbitMappedPort;

    public static async Task AcquireMongoAsync(CancellationToken cancellationToken = default)
    {
        await Gate.WaitAsync(cancellationToken);
        try
        {
            _mongoUsers++;
            if (await EnsureContainerRunningAsync(_mongo))
                return;

            _mongo = null;
            _mongoConnectionString = null;

            _mongo = new ContainerBuilder()
                .WithImage("mongo:8")
                // Docker Desktop Linux VM on current macOS: mongo:8 needs glibc rseq on.
                // Harmless on Windows / Linux amd64.
                .WithEnvironment("GLIBC_TUNABLES", "glibc.pthread.rseq=1")
                .WithCommand("--replSet", MongoReplicaSetName, "--bind_ip_all")
                .WithPortBinding(MongoPort, true)
                .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(MongoPort))
                .Build();

            await StartContainerWithRetryAsync(_mongo, cancellationToken);
            await InitiateReplicaSetAsync(_mongo, cancellationToken);

            var host = _mongo.Hostname;
            var port = _mongo.GetMappedPublicPort(MongoPort);
            _mongoConnectionString = $"mongodb://{host}:{port}/?replicaSet={MongoReplicaSetName}&directConnection=true";
            await WaitUntilPrimaryAsync(_mongoConnectionString, cancellationToken);
        }
        catch
        {
            _mongoUsers--;
            if (_mongo is not null)
            {
                try { await _mongo.DisposeAsync(); } catch { /* ignore */ }
                _mongo = null;
                _mongoConnectionString = null;
            }

            throw;
        }
        finally
        {
            Gate.Release();
        }
    }

    public static async Task ReleaseMongoAsync()
    {
        await Gate.WaitAsync();
        try
        {
            if (_mongoUsers == 0)
                return;

            if (--_mongoUsers != 0)
                return;

            if (_mongo is not null)
            {
                await _mongo.DisposeAsync();
                _mongo = null;
            }

            _mongoConnectionString = null;
        }
        finally
        {
            Gate.Release();
        }
    }

    public static async Task AcquireSqlAsync(CancellationToken cancellationToken = default)
    {
        await Gate.WaitAsync(cancellationToken);
        try
        {
            _sqlUsers++;
            if (await EnsureContainerRunningAsync(_sql) && _sqlConnectionString is not null)
                return;

            _sql = null;
            _sqlConnectionString = null;

            _sql = new ContainerBuilder()
                .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
                .WithEnvironment("ACCEPT_EULA", "Y")
                .WithEnvironment("MSSQL_PID", "Developer")
                .WithEnvironment("SA_PASSWORD", SqlSaPassword)
                .WithEnvironment("MSSQL_SA_PASSWORD", SqlSaPassword)
                // Official image is amd64-only. Required on Apple Silicon (Rosetta);
                // no-op on Windows x64. 2 GB cap matches local compose.
                .WithCreateParameterModifier(parameters =>
                {
                    parameters.Platform = "linux/amd64";
                    parameters.HostConfig ??= new HostConfig();
                    parameters.HostConfig.Memory = 2L * 1024 * 1024 * 1024;
                })
                .WithPortBinding(SqlPort, assignRandomHostPort: true)
                .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(SqlPort))
                .Build();

            await StartContainerWithRetryAsync(_sql, cancellationToken);

            var host = _sql.Hostname;
            var port = _sql.GetMappedPublicPort(SqlPort);
            var masterBuilder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder
            {
                DataSource = $"{host},{port}",
                UserID = "sa",
                Password = SqlSaPassword,
                InitialCatalog = "master",
                Encrypt = false,
                TrustServerCertificate = true,
                ConnectTimeout = 5
            };

            await WaitForSqlServerReadyAsync(masterBuilder.ConnectionString, cancellationToken);
            await EnsureSqlDatabaseExistsAsync(masterBuilder.ConnectionString, cancellationToken);

            masterBuilder.InitialCatalog = SqlDatabaseName;
            _sqlConnectionString = masterBuilder.ConnectionString;
        }
        catch
        {
            _sqlUsers--;
            if (_sql is not null)
            {
                try { await _sql.DisposeAsync(); } catch { /* ignore */ }
                _sql = null;
                _sqlConnectionString = null;
            }

            throw;
        }
        finally
        {
            Gate.Release();
        }
    }

    public static async Task ReleaseSqlAsync()
    {
        await Gate.WaitAsync();
        try
        {
            if (_sqlUsers == 0)
                return;

            if (--_sqlUsers != 0)
                return;

            if (_sql is not null)
            {
                await _sql.DisposeAsync();
                _sql = null;
            }

            _sqlConnectionString = null;
        }
        finally
        {
            Gate.Release();
        }
    }

    public static async Task AcquireRabbitAsync(CancellationToken cancellationToken = default)
    {
        await Gate.WaitAsync(cancellationToken);
        try
        {
            _rabbitUsers++;
            if (await EnsureContainerRunningAsync(_rabbit))
                return;

            _rabbit = null;
            _rabbitHost = null;
            _rabbitMappedPort = 0;

            _rabbit = new ContainerBuilder()
                .WithImage("rabbitmq:3-management")
                .WithPortBinding(RabbitPort, true)
                .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(RabbitPort))
                .Build();

            await StartContainerWithRetryAsync(_rabbit, cancellationToken);
            CaptureRabbitEndpoint();
            await WaitUntilRabbitAcceptsConnectionsAsync(cancellationToken);
        }
        catch
        {
            _rabbitUsers--;
            if (_rabbit is not null)
            {
                try { await _rabbit.DisposeAsync(); } catch { /* ignore */ }
                _rabbit = null;
                _rabbitHost = null;
                _rabbitMappedPort = 0;
            }

            throw;
        }
        finally
        {
            Gate.Release();
        }
    }

    public static async Task ReleaseRabbitAsync()
    {
        await Gate.WaitAsync();
        try
        {
            if (_rabbitUsers == 0)
                return;

            if (--_rabbitUsers != 0)
                return;

            if (_rabbit is not null)
            {
                await _rabbit.DisposeAsync();
                _rabbit = null;
            }

            _rabbitHost = null;
            _rabbitMappedPort = 0;
        }
        finally
        {
            Gate.Release();
        }
    }

    public static async Task StopRabbitAsync(CancellationToken cancellationToken = default)
    {
        if (_rabbit is null)
            throw new InvalidOperationException("RabbitMQ Testcontainer is not started.");

        if (_rabbit.State != TestcontainersStates.Running)
            return;

        await _rabbit.StopAsync(cancellationToken);
    }

    public static async Task StartRabbitAsync(CancellationToken cancellationToken = default)
    {
        if (_rabbit is null)
            throw new InvalidOperationException("RabbitMQ Testcontainer is not started.");

        if (_rabbit.State != TestcontainersStates.Running)
            await _rabbit.StartAsync(cancellationToken);

        CaptureRabbitEndpoint();
        await WaitUntilRabbitAcceptsConnectionsAsync(cancellationToken);
    }

    public static ConnectionFactory CreateRabbitConnectionFactory() =>
        new()
        {
            HostName = RabbitHost,
            Port = RabbitMappedPort,
            UserName = "guest",
            Password = "guest"
        };

    private static void CaptureRabbitEndpoint()
    {
        if (_rabbit is null)
            return;

        _rabbitHost = _rabbit.Hostname;
        _rabbitMappedPort = (ushort)_rabbit.GetMappedPublicPort(RabbitPort);
    }

    private static async Task<bool> EnsureContainerRunningAsync(IContainer? container)
    {
        if (container is null)
            return false;

        if (container.State == TestcontainersStates.Running)
            return true;

        try
        {
            await container.DisposeAsync();
        }
        catch
        {
            // container already gone from Docker
        }

        return false;
    }

    private static async Task StartContainerWithRetryAsync(IContainer container, CancellationToken cancellationToken)
    {
        const int maxAttempts = 3;
        Exception? lastError = null;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await container.StartAsync(cancellationToken);
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts && IsTransientDockerFault(ex))
            {
                lastError = ex;
                await Task.Delay(TimeSpan.FromSeconds(2 * attempt), cancellationToken);
            }
        }

        throw lastError ?? new InvalidOperationException("Failed to start Testcontainer.");
    }

    private static bool IsTransientDockerFault(Exception ex)
    {
        var text = ex.ToString();
        return text.Contains("Unexpected end of stream", StringComparison.OrdinalIgnoreCase)
            || text.Contains("DockerContainer", StringComparison.OrdinalIgnoreCase)
            || text.Contains("No such container", StringComparison.OrdinalIgnoreCase)
            || text.Contains("named pipe", StringComparison.OrdinalIgnoreCase);
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

    private static async Task EnsureSqlDatabaseExistsAsync(string masterConnectionString, CancellationToken cancellationToken)
    {
        await using var connection = new Microsoft.Data.SqlClient.SqlConnection(masterConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = $"IF DB_ID(N'{SqlDatabaseName}') IS NULL CREATE DATABASE [{SqlDatabaseName}]";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task InitiateReplicaSetAsync(IContainer mongo, CancellationToken cancellationToken)
    {
        var eval =
            "try { rs.status() } catch (e) { rs.initiate({ _id: 'rs0', members: [{ _id: 0, host: '127.0.0.1:27017' }] }) }";

        var result = await mongo.ExecAsync(["mongosh", "--quiet", "--eval", eval], cancellationToken);
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Failed to initiate Mongo replica set: {result.Stderr}{result.Stdout}");
        }
    }

    private static async Task WaitUntilPrimaryAsync(string connectionString, CancellationToken cancellationToken)
    {
        var client = new MongoClient(connectionString);
        var deadline = DateTime.UtcNow.AddMinutes(2);

        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var admin = client.GetDatabase("admin");
                var status = await admin.RunCommandAsync<BsonDocument>(
                    new BsonDocument("replSetGetStatus", 1),
                    cancellationToken: cancellationToken);

                if (status.GetValue("ok", 0).ToInt32() == 1
                    && status.GetValue("members", new BsonArray()).AsBsonArray.Any(m => m["stateStr"] == "PRIMARY"))
                {
                    return;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                // replica set still electing
            }

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }

        throw new InvalidOperationException("MongoDB Testcontainer replica set did not become PRIMARY in time.");
    }

    private static async Task WaitUntilRabbitAcceptsConnectionsAsync(CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow.AddMinutes(2);
        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var factory = CreateRabbitConnectionFactory();
                await using var connection = await factory.CreateConnectionAsync(cancellationToken);
                await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
                return;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }

        throw new InvalidOperationException("RabbitMQ Testcontainer did not accept connections in time.");
    }
}
