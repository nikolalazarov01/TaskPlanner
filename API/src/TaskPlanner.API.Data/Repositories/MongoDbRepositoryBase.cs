using MongoDB.Driver;
using OneBitSoftware.Utilities;
using TaskPlanner.API.Data.Interfaces;
using TaskPlanner.API.Data.Models;
using TaskPlanner.API.Utilities;

namespace TaskPlanner.API.Data.Repositories;

public class MongoDbRepositoryBase<TEntity> : IBaseRepository<TEntity>
    where TEntity : IEntity
{
    public MongoDbRepositoryBase(IMongoDatabase database, string collectionName)
    {
        Database = database ?? throw new ArgumentNullException(nameof(database));

        if (string.IsNullOrWhiteSpace(collectionName))
        {
            throw new ArgumentException("Collection name must be provided.", nameof(collectionName));
        }

        Collection = database.GetCollection<TEntity>(collectionName);
    }

    protected IMongoDatabase Database { get; }

    protected IMongoCollection<TEntity> Collection { get; }
    
    public async Task<OperationResult> CreateAsync(TEntity entity)
    {
        var operationResult = new OperationResult();
        try
        {
            await Collection.InsertOneAsync(entity);
        }
        catch (MongoWriteException e) when (e.WriteError is not null && e.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            var error = new DuplicateKeyError(e.WriteError.Message);
            operationResult.AppendError(error);
        }

        return operationResult;
    }
}

