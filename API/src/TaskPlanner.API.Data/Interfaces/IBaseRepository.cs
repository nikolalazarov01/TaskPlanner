using MongoDB.Driver;
using OneBitSoftware.Utilities;

namespace TaskPlanner.API.Data.Interfaces;

public interface IBaseRepository<TEntity> where TEntity : IEntity
{
    Task<OperationResult<TEntity>> CreateAsync(TEntity entity);
    Task<OperationResult<TEntity>> ModifyAsync(TEntity entity, CancellationToken cancellationToken, UpdateDefinition<TEntity> update = null);
    Task<OperationResult<IReadOnlyList<TEntity>>> GetAsync(FilterDefinition<TEntity> filter, CancellationToken cancellationToken, SortDefinition<TEntity>? sort = null, int? skip = null, int? limit = null);
    Task<OperationResult<TEntity>> GetOneAsync(FilterDefinition<TEntity> filter, CancellationToken cancellationToken);
}

