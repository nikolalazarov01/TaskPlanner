using System;
using Mongo2Go;
using MongoDB.Driver;
using Xunit;

public class Mongo2GoFixture : IAsyncLifetime
{
    private MongoDbRunner _runner;

    public IMongoDatabase Database { get; private set; }
    public IMongoClient Client { get; private set; }

    public string DatabaseName { get; } = "integration-tests";

    public async Task InitializeAsync()
    {
        var external = Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING");

        string connectionString;
        if (!string.IsNullOrWhiteSpace(external))
        {
            connectionString = external; // use CI container
        }
        else
        {
            _runner = MongoDbRunner.Start(singleNodeReplSet: false);
            connectionString = _runner.ConnectionString;
        }

        Client = new MongoClient(connectionString);
        Database = Client.GetDatabase(DatabaseName);

        // Simple readiness check (throws if not reachable)
        await Database.RunCommandAsync<MongoDB.Bson.BsonDocument>(new MongoDB.Bson.BsonDocument("ping", 1));
    }

    public Task DisposeAsync()
    {
        _runner?.Dispose();
        return Task.CompletedTask;
    }
}