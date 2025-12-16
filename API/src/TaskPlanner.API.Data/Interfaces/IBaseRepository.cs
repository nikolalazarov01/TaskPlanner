using OneBitSoftware.Utilities;
using TaskPlanner.API.Data.Models;

namespace TaskPlanner.API.Data.Interfaces;

public interface IBaseRepository<TEntity> where TEntity : IEntity
{
    Task<OperationResult> CreateAsync(TEntity entity);
}

