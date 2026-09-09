namespace IntegrationTests.Domains.Messaging;

using MongoDB.Bson;
using MongoDB.Driver;
using RabbitMQ.Client;

public sealed class MessagingInfraFixture : IAsyncLifetime
{
    public const string MongoConnectionString = "mongodb://127.0.0.1:27017/?replicaSet=rs0&directConnection=true";
    public const string RabbitHost = "127.0.0.1";

    public async Task InitializeAsync()
    {
        await WaitForMongoReplicaSetAsync();
        await WaitForRabbitMqAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static async Task WaitForMongoReplicaSetAsync()
    {
        var client = new MongoClient(MongoConnectionString);
        var deadline = DateTime.UtcNow.AddMinutes(2);

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var admin = client.GetDatabase("admin");
                var status = await admin.RunCommandAsync<BsonDocument>(new BsonDocument("replSetGetStatus", 1));
                if (status.GetValue("ok", 0).ToInt32() == 1
                    && status.GetValue("members", new BsonArray()).AsBsonArray.Any(m => m["stateStr"] == "PRIMARY"))
                {
                    return;
                }
            }
            catch
            {
                // retry until replica set is ready
            }

            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        throw new InvalidOperationException(
            "MongoDB replica set is not ready. Run: docker compose -f src/docker-compose.yml up -d mongo rabbitmq");
    }

    private static async Task WaitForRabbitMqAsync()
    {
        var deadline = DateTime.UtcNow.AddMinutes(2);

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = RabbitHost,
                    UserName = "guest",
                    Password = "guest"
                };

                await using var connection = await factory.CreateConnectionAsync();
                await using var channel = await connection.CreateChannelAsync();
                return;
            }
            catch
            {
                await Task.Delay(TimeSpan.FromSeconds(2));
            }
        }

        throw new InvalidOperationException(
            "RabbitMQ is not ready. Run: docker compose -f src/docker-compose.yml up -d mongo rabbitmq");
    }
}
