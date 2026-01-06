using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using TaskPlanner.API.Data.Interfaces;
using TaskPlanner.API.Data.Transactions;

namespace TaskPlanner.API.Data.Configuration;

public static class ServiceConfigurationExtensions
{
    public static void SetupDataServices(this IServiceCollection services)
    {
        services.AddSingleton<ITransactionsContainer<IClientSessionHandle>, TransactionsContainer<IClientSessionHandle>>();
        services.AddSingleton<ITransactionManager, MongoTransactionManager>();
        services.AddScoped<ITransactionManagementUtility, TransactionManagementUtility>();

    }
}