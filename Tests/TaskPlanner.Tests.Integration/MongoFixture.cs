using System;
using Mongo2Go;
using MongoDB.Driver;
using Xunit;

public class Mongo2GoFixture : IAsyncLifetime
{
    private MongoDbRunner _runner;
    public IMongoDatabase Database { get; private set; }
    public IMongoClient Client { get; private set; }

    public string ConnectionString => _runner?.ConnectionString;
    public string DatabaseName { get; } = "integration-tests";

    public async Task InitializeAsync()
    {
        _runner = MongoDbRunner.Start(singleNodeReplSet: true); // enables transactions if needed
        Client = new MongoClient(_runner.ConnectionString);
        Database = Client.GetDatabase(DatabaseName);

        // Optional: seed data here
        // var collection = Database.GetCollection<MyEntity>("MyCollection");
        // await collection.InsertOneAsync(new MyEntity { ... });
    }

    public Task DisposeAsync()
    {
        _runner?.Dispose();
        return Task.CompletedTask;
    }
}