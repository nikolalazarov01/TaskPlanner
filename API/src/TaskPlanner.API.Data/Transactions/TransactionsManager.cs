using MongoDB.Driver;
using TaskPlanner.API.Data.Interfaces;

namespace TaskPlanner.API.Data.Transactions;

public sealed class MongoTransactionManager : ITransactionManager
{
    private readonly IMongoClient _client;
    private readonly ITransactionsContainer<IClientSessionHandle> _container;

    private const string DefaultKey = "mongo"; // single-db app

    public MongoTransactionManager(IMongoClient client, ITransactionsContainer<IClientSessionHandle> container)
    {
        _client = client;
        _container = container;
    }

    public async Task<bool> BeginAsync(CancellationToken ct)
    {
        if (_container.Get(DefaultKey) != null)
            return false; // already active

        var session = await _client.StartSessionAsync(cancellationToken: ct);
        session.StartTransaction();
        _container.Set(DefaultKey, session);
        return true;
    }

    public async Task CommitAsync(CancellationToken ct)
    {
        var session = _container.Take(DefaultKey);
        if (session == null) return;

        await session.CommitTransactionAsync(ct);
        session.Dispose();
    }

    public async Task AbortAsync(CancellationToken ct)
    {
        var session = _container.Take(DefaultKey);
        if (session == null) return;

        await session.AbortTransactionAsync(ct);
        session.Dispose();
    }

    public IClientSessionHandle? CurrentSession()
        => _container.Get(DefaultKey);
}