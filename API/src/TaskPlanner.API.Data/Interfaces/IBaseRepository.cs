using MongoDB.Driver;
using OneBitSoftware.Utilities;

namespace TaskPlanner.API.Data.Interfaces;

public interface IBaseRepository<TEntity> where TEntity : IEntity
{
    Task<OperationResult<TEntity>> CreateAsync(TEntity entity);
    Task<OperationResult<TEntity>> UpdateAsync(TEntity entity, CancellationToken cancellationToken, UpdateDefinition<TEntity> update = null);
}

