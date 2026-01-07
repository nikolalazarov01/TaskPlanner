using TaskPlanner.API.Data.Attributes;
using TaskPlanner.API.Data.Interfaces;

namespace TaskPlanner.API.Data.Extensions;

public static class RepositoryExtensions
{
    public static string ResolveCollectionName<TEntity>(this IBaseRepository<TEntity> repository)
        where TEntity : IEntity
    {
        var attr = typeof(TEntity)
            .GetCustomAttributes(typeof(MongoCollectionAttribute), inherit: false)
            .FirstOrDefault() as MongoCollectionAttribute;

        return attr?.Name ?? typeof(TEntity).Name;
    }   
}