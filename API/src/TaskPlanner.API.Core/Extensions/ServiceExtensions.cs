using MongoDB.Driver;
using TaskPlanner.API.Data.Interfaces;

namespace TaskPlanner.API.Core.Extensions;

public static class ServiceExtensions
{
    public static UpdateDefinition<TEntity> AddUpdateDefinition<TEntity>(this UpdateDefinition<TEntity>? current, UpdateDefinition<TEntity> next)
        where TEntity : IEntity
    {
        return current is null ? next : Builders<TEntity>.Update.Combine(current, next);
    }
    
    public static bool AllSame<TSource, TValue>(this IReadOnlyCollection<TSource> source, Func<TSource, TValue> selector)
    {
        if (source is null || source.Count == 0)
            return false;

        using var enumerator = source.GetEnumerator();
        enumerator.MoveNext();

        var firstValue = selector(enumerator.Current);
        var comparer = EqualityComparer<TValue>.Default;

        while (enumerator.MoveNext())
        {
            if (!comparer.Equals(firstValue, selector(enumerator.Current)))
                return false;
        }

        return true;
    }
}