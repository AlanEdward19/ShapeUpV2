namespace IntegrationTests.Infrastructure;

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using MongoDB.Bson;
using MongoDB.Driver;
using RabbitMQ.Client;

public static class IntegrationTestContainers
{
    private const string MongoReplicaSetName = "rs0";
    private const ushort MongoPort = 27017;
    private const ushort RabbitPort = 5672;

    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static IContainer? _mongo;
    private static IContainer? _rabbit;
    private static int _mongoUsers;
    private static int _rabbitUsers;
    private static string? _mongoConnectionString;
    private static string? _rabbitHost;
    private static ushort _rabbitMappedPort;

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
            if (_mongo is not null)
                return;

            _mongo = new ContainerBuilder()
                .WithImage("mongo:8")
                .WithCommand("--replSet", MongoReplicaSetName, "--bind_ip_all")
                .WithPortBinding(MongoPort, true)
                .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(MongoPort))
                .Build();

            await _mongo.StartAsync(cancellationToken);
            await InitiateReplicaSetAsync(_mongo, cancellationToken);

            var host = _mongo.Hostname;
            var port = _mongo.GetMappedPublicPort(MongoPort);
            _mongoConnectionString = $"mongodb://{host}:{port}/?replicaSet={MongoReplicaSetName}&directConnection=true";
            await WaitUntilPrimaryAsync(_mongoConnectionString, cancellationToken);
        }
        catch
        {
            _mongoUsers--;
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

    public static async Task AcquireRabbitAsync(CancellationToken cancellationToken = default)
    {
        await Gate.WaitAsync(cancellationToken);
        try
        {
            _rabbitUsers++;
            if (_rabbit is not null)
                return;

            _rabbit = new ContainerBuilder()
                .WithImage("rabbitmq:3-management")
                .WithPortBinding(RabbitPort, true)
                .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(RabbitPort))
                .Build();

            await _rabbit.StartAsync(cancellationToken);
            CaptureRabbitEndpoint();
            await WaitUntilRabbitAcceptsConnectionsAsync(cancellationToken);
        }
        catch
        {
            _rabbitUsers--;
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
