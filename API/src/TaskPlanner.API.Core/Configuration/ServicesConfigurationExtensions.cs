using Microsoft.Extensions.DependencyInjection;
using TaskPlanner.API.Core.Interfaces;
using TaskPlanner.API.Core.Services;
using TaskPlanner.API.Data.Interfaces;
using TaskPlanner.API.Data.Repositories;

namespace TaskPlanner.API.Core.Configuration;

public static class ServicesConfigurationExtensions
{
    public static void SetupServices(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<IUserRepository, UserRepository>();
        serviceCollection.AddScoped<IIdentityService, IdentityService>();
    }
}