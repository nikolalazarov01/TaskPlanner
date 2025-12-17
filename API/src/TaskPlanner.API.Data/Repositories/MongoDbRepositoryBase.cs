using MongoDB.Driver;
using OneBitSoftware.Utilities;
using OneBitSoftware.Utilities.Errors;
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
    
    public async Task<OperationResult<TEntity>> CreateAsync(TEntity entity)
    {
        var operationResult = new OperationResult<TEntity>();
        try
        {
            await Collection.InsertOneAsync(entity);
        }
        catch (MongoWriteException e) when (e.WriteError is not null && e.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            var error = new DuplicateKeyError(e.WriteError.Message);
            operationResult.AppendError(error);
        }

        return operationResult.WithRelatedObject(entity);
    }
    
    public async Task<OperationResult<TEntity>> UpdateAsync(TEntity entity)
    {
        var result = new OperationResult<TEntity>();

        try
        {
            var filter = Builders<TEntity>.Filter.Eq(e => e.Id, entity.Id);

            var options = new ReplaceOptions
            {
                IsUpsert = false
            };

            var replaceResult = await Collection.ReplaceOneAsync(filter, entity, options);

            if (replaceResult.MatchedCount == 0)
            {
                result.AppendError(new OperationError(
                    $"Entity with id '{entity.Id}' was not found."));
                return result;
            }
        }
        catch (MongoWriteException e) when
            (e.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            result.AppendError(new DuplicateKeyError(e.WriteError.Message));
            return result;
        }

        return result.WithRelatedObject(entity);
    }
}

