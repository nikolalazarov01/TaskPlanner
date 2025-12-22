using MongoDB.Driver;
using TaskPlanner.API.Data.Interfaces;
using TaskPlanner.API.Data.Repositories;
using TaskPlanner.API.Data.Transactions;

namespace TaskPlanner.Tests.Integration;

public static class TestPreparationData
{
    public static MongoDbRepositoryBase<TEntity> CreateRepository<TEntity>(Mongo2GoFixture mongoFixture)
        where TEntity : IEntity
    {
        var (txManager, _) = CreateTransactionManagementComponents(mongoFixture);
        
        return new MongoDbRepositoryBase<TEntity>(mongoFixture.Database, txManager);
    }

    public static (MongoTransactionManager, TransactionManagementUtility) CreateTransactionManagementComponents(Mongo2GoFixture mongoFixture)
    {
        var txContainer = new TransactionsContainer<IClientSessionHandle>();
        var txManager = new MongoTransactionManager(mongoFixture.Client, txContainer);
        var txUtility = new TransactionManagementUtility(txManager);

        return (txManager, txUtility);
    }
}