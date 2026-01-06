using System.Collections.Concurrent;
using TaskPlanner.API.Data.Interfaces;

namespace TaskPlanner.API.Data.Transactions;

/// <inheritdoc/>
public sealed class TransactionsContainer<TSession> : ITransactionsContainer<TSession>
    where TSession : class
{
    private readonly ConcurrentDictionary<string, TSession> _sessions = new();

    /// <inheritdoc/>
    public TSession? Get(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;
        _sessions.TryGetValue(key, out var session);
        return session;
    }

    /// <inheritdoc/>
    public bool Set(string key, TSession session)
    {
        if (string.IsNullOrWhiteSpace(key)) return false;
        if (session is null) return false;

        _sessions[key] = session;
        return true;
    }

    /// <inheritdoc/>
    public bool Remove(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return false;
        return _sessions.TryRemove(key, out _);
    }

    /// <inheritdoc/>
    public TSession? Take(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;
        return _sessions.TryRemove(key, out var session) ? session : null;
    }
}