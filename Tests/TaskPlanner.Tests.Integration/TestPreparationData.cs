using MongoDB.Driver;
using TaskPlanner.API.Data.Interfaces;
using TaskPlanner.API.Data.Models;
using TaskPlanner.API.Data.Repositories;
using TaskPlanner.API.Data.Transactions;

namespace TaskPlanner.Tests.Integration;

public static class TestPreparationData
{
    public static (MongoTransactionManager txManager, TransactionManagementUtility txUtility, IBaseRepository<Category> categoriesRepo, IBaseRepository<TaskPlanner.API.Data.Models.Task> tasksRepo) CreateInfra(Mongo2GoFixture fixture)
    {
        var txContainer = new TransactionsContainer<IClientSessionHandle>();
        var txManager = new MongoTransactionManager(fixture.Client, txContainer);
        var txUtility = new TransactionManagementUtility(txManager);

        // IMPORTANT: repos must use THE SAME txManager
        var categoriesRepo = new MongoDbRepositoryBase<Category>(fixture.Database, txManager);
        var tasksRepo = new MongoDbRepositoryBase<TaskPlanner.API.Data.Models.Task>(fixture.Database, txManager);

        return (txManager, txUtility, categoriesRepo, tasksRepo);
    }
    
    public static MongoDbRepositoryBase<TEntity> CreateRepository<TEntity>(
        Mongo2GoFixture mongoFixture,
        MongoTransactionManager? txManager = null)
        where TEntity : IEntity
    {
        txManager ??= CreateTransactionManagementComponents(mongoFixture).Item1;
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