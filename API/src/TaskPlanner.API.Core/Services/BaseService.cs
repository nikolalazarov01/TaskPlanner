using MongoDB.Bson;
using TaskPlanner.API.Core.Interfaces;
using TaskPlanner.API.Data.Interfaces;

namespace TaskPlanner.API.Core.Services;

public abstract class BaseService<TEntity> : IService<TEntity> where TEntity : IEntity
{
    public virtual Task<TEntity> CreateAsync(TEntity entity)
    {
        throw new NotImplementedException();
    }

    public virtual Task DeleteAsync(ObjectId entityId)
    {
        throw new NotImplementedException();
    }

    public virtual Task<TEntity?> GetAsync(ObjectId entityId)
    {
        throw new NotImplementedException();
    }

    public virtual Task<TEntity> UpdateAsync(TEntity entity)
    {
        throw new NotImplementedException();
    }
}

