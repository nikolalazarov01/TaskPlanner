using System.Collections.Concurrent;
using MongoDB.Bson;
using TaskPlanner.API.Data.Interfaces;

namespace TestPlanner.Tests.Integration;

public sealed class InMemoryDatabase
{
    private readonly ConcurrentDictionary<ObjectId, object> _store = new();

    public TEntity? Get<TEntity>(ObjectId id) where TEntity : class, IEntity
    {
        return _store.TryGetValue(id, out var entity)
            ? entity as TEntity
            : null;
    }

    public void Save<TEntity>(TEntity entity) where TEntity : class, IEntity
    {
        _store[entity.Id] = entity;
    }
}
