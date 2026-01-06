using MongoDB.Driver;
using OneBitSoftware.Utilities;
using TaskPlanner.API.Data.Extensions;
using TaskPlanner.API.Data.Interfaces;
using TaskPlanner.API.Utilities;

namespace TaskPlanner.API.Data.Repositories;

/// <inheritdoc/>
public class MongoDbRepositoryBase<TEntity> : IBaseRepository<TEntity>
    where TEntity : IEntity
{
    private readonly ITransactionManager _transactionsManager;
    
    public MongoDbRepositoryBase(IMongoDatabase database, ITransactionManager transactionsManager, string? collectionName = null)
    {
        Database = database ?? throw new ArgumentNullException(nameof(database));
        _transactionsManager = transactionsManager;

        collectionName ??= this.ResolveCollectionName();

        Collection = database.GetCollection<TEntity>(collectionName);
    }

    protected IMongoDatabase Database { get; }

    private IMongoCollection<TEntity> Collection { get; }
    
    private IClientSessionHandle? Session => _transactionsManager.CurrentSession();
    
    /// <inheritdoc/>
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
    
    /// <inheritdoc/>
    public async Task<OperationResult<TEntity>> UpdateAsync(TEntity entity, CancellationToken cancellationToken)
    {
        var result = new OperationResult<TEntity>();

        try
        {
            var filter = Builders<TEntity>.Filter.Eq(e => e.Id, entity.Id);

            var options = new FindOneAndReplaceOptions<TEntity>
            {
                IsUpsert = false,
                ReturnDocument = ReturnDocument.After
            };

            var updatedEntity = await Collection.FindOneAndReplaceAsync(filter, entity, options, cancellationToken);

            if (updatedEntity is null) return result.AppendError("Entity not found.");;
            
            return result.WithRelatedObject(updatedEntity);
        }
        catch (MongoWriteException e) when
            (e.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            result.AppendError(new DuplicateKeyError(e.WriteError.Message));
            return result;
        }
        catch (Exception ex)
        {
            result.AppendError(ex.Message);
            return result;
        }
    }

    /// <inheritdoc/>
    public async Task<OperationResult<long>> UpdateManyAsync(FilterDefinition<TEntity> filter, UpdateDefinition<TEntity> update, CancellationToken cancellationToken)
    {
        var result = new OperationResult<long>();

        try
        {
            if (filter is null)
            {
                filter = Builders<TEntity>.Filter.Empty;
            }

            if (update is null)
            {
                result.AppendError("Update definition must be provided.");
                return result;
            }

            var updateResult = await Collection.UpdateManyAsync(
                filter,
                update,
                cancellationToken: cancellationToken);

            // Optional "not found" semantics
            if (updateResult.MatchedCount == 0)
            {
                result.AppendError(new NotFoundError("No entities found to update."));
                return result;
            }

            return result.WithRelatedObject(updateResult.ModifiedCount);
        }
        catch (MongoWriteException e) when
            (e.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            result.AppendError(new DuplicateKeyError(e.WriteError.Message));
            return result;
        }
        catch (Exception ex)
        {
            result.AppendError(ex.Message);
            return result;
        }
    }
    
    /// <inheritdoc/>
    public async Task<OperationResult<TEntity>> ModifyAsync(TEntity entity, CancellationToken cancellationToken, UpdateDefinition<TEntity> update = null)
    {
        var result = new OperationResult<TEntity>();

        try
        {
            var filter = Builders<TEntity>.Filter.Eq(e => e.Id, entity.Id);

            var options = new FindOneAndUpdateOptions<TEntity>
            {
                IsUpsert = false,
                ReturnDocument = ReturnDocument.After
            };

            var updatedEntity = await Collection.FindOneAndUpdateAsync(filter, update, options, cancellationToken);

            if (updatedEntity is null)
            {
                result.AppendError("Entity not found.");
                return result;
            }
            
            return result.WithRelatedObject(updatedEntity);
        }
        catch (MongoWriteException e) when
            (e.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            result.AppendError(new DuplicateKeyError(e.WriteError.Message));
            return result;
        }
    }
    
    /// <inheritdoc/>
    public async Task<OperationResult<long>> ModifyManyAsync(FilterDefinition<TEntity> filter, UpdateDefinition<TEntity> update, CancellationToken cancellationToken, bool isUpsert = false)
    {
        var result = new OperationResult<long>();

        try
        {
            filter ??= Builders<TEntity>.Filter.Empty;

            if (update is null)
            {
                result.AppendError("Update definition must be provided.");
                return result;
            }

            var options = new UpdateOptions
            {
                IsUpsert = isUpsert
            };

            var updateResult = await Collection.UpdateManyAsync(
                filter,
                update,
                options,
                cancellationToken);

            // Optional "not found" semantics (only when not upserting)
            if (!isUpsert && updateResult.MatchedCount == 0)
            {
                result.AppendError(new NotFoundError("No entities found to modify."));
                return result;
            }

            return result.WithRelatedObject(updateResult.ModifiedCount);
        }
        catch (MongoWriteException e) when
            (e.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            result.AppendError(new DuplicateKeyError(e.WriteError.Message));
            return result;
        }
        catch (Exception ex)
        {
            result.AppendError(ex.Message);
            return result;
        }
    }
    
    /// <inheritdoc/>
    public async Task<OperationResult<IReadOnlyList<TEntity>>> GetAsync(FilterDefinition<TEntity> filter, CancellationToken cancellationToken, SortDefinition<TEntity>? sort = null, int? skip = null, int? limit = null)
    {
        var result = new OperationResult<IReadOnlyList<TEntity>>();

        try
        {
            filter ??= Builders<TEntity>.Filter.Empty;

            var query = Collection.Find(filter);

            if (sort is not null)
            {
                query = query.Sort(sort);
            }

            if (skip.HasValue)
            {
                query = query.Skip(skip.Value);
            }

            if (limit.HasValue)
            {
                query = query.Limit(limit.Value);
            }

            var entities = await query.ToListAsync(cancellationToken);

            return result.WithRelatedObject(entities);
        }
        catch (Exception ex)
        {
            result.AppendError(ex.Message);
            return result;
        }
    }
    
    /// <inheritdoc/>
    public async Task<OperationResult<TEntity>> GetOneAsync(FilterDefinition<TEntity> filter, CancellationToken cancellationToken)
    {
        var result = new OperationResult<TEntity>();

        try
        {
            filter ??= Builders<TEntity>.Filter.Empty;

            var entity = await Collection.Find(filter).FirstOrDefaultAsync(cancellationToken);

            if (entity is null)
            {
                result.AppendError(new NotFoundError("Entity not found."));
                return result;
            }

            return result.WithRelatedObject(entity);
        }
        catch (Exception ex)
        {
            result.AppendError(ex.Message);
            return result;
        }
    }
    
    /// <inheritdoc/>
    public async Task<OperationResult<TEntity>> DeleteOneAsync(FilterDefinition<TEntity> filter, CancellationToken cancellationToken)
    {
        var result = new OperationResult<TEntity>();

        try
        {
            filter ??= Builders<TEntity>.Filter.Empty;

            // Return deleted entity (useful for controller/service mapping)
            var deleted = Session is null
                ? await Collection.FindOneAndDeleteAsync(filter, cancellationToken: cancellationToken)
                : await Collection.FindOneAndDeleteAsync(Session, filter, cancellationToken: cancellationToken);

            if (deleted is null)
            {
                result.AppendError(new NotFoundError("Entity not found."));
                return result;
            }

            return result.WithRelatedObject(deleted);
        }
        catch (Exception ex)
        {
            result.AppendError(ex.Message);
            return result;
        }
    }

    /// <inheritdoc/>
    public async Task<OperationResult<long>> DeleteManyAsync(FilterDefinition<TEntity> filter, CancellationToken cancellationToken)
    {
        var result = new OperationResult<long>();

        try
        {
            filter ??= Builders<TEntity>.Filter.Empty;

            var deleteResult = Session is null
                ? await Collection.DeleteManyAsync(filter, cancellationToken)
                : await Collection.DeleteManyAsync(Session, filter, cancellationToken: cancellationToken);

            // If you want "not found" semantics when nothing was deleted:
            if (deleteResult.DeletedCount == 0) return result;
            

            return result.WithRelatedObject(deleteResult.DeletedCount);
        }
        catch (Exception ex)
        {
            result.AppendError(ex.Message);
            return result;
        }
    }
    
    /// <inheritdoc/>
    public async Task<OperationResult<bool>> AnyAsync(FilterDefinition<TEntity> filter, CancellationToken cancellationToken)
    {
        var result = new OperationResult<bool>();

        try
        {
            filter ??= Builders<TEntity>.Filter.Empty;

            var any = await Collection.Find(filter).AnyAsync(cancellationToken);

            return result.WithRelatedObject(any);
        }
        catch (Exception ex)
        {
            result.AppendError(ex.Message);
            return result;
        }
    }
}

