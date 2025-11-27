using MongoDB.Driver;
using TaskPlanner.API.Data.Interfaces;

namespace TaskPlanner.API.Data.Repositories;

public abstract class MongoDbRepositoryBase<TDocument> : IBaseRepository
{
    protected MongoDbRepositoryBase(IMongoDatabase database, string collectionName)
    {
        Database = database ?? throw new ArgumentNullException(nameof(database));

        if (string.IsNullOrWhiteSpace(collectionName))
        {
            throw new ArgumentException("Collection name must be provided.", nameof(collectionName));
        }

        Collection = database.GetCollection<TDocument>(collectionName);
    }

    protected IMongoDatabase Database { get; }

    protected IMongoCollection<TDocument> Collection { get; }
}

