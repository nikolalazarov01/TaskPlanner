using MongoDB.Bson;
using TaskPlanner.API.Data.Interfaces;

namespace TaskPlanner.API.Core.Interfaces;

public interface IService<TEntity> where TEntity : IEntity
{
    Task<TEntity> CreateAsync(TEntity entity);

    Task<TEntity> UpdateAsync(TEntity entity);

    Task DeleteAsync(ObjectId entityId);

    Task<TEntity?> GetAsync(ObjectId entityId);
}

