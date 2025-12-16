using System.Reflection;
using FluentValidation;

namespace TaskPlanner.API.Web.Configuration;

public static class ValidatorConfigurationExtensions
{
    public static void SetupValidation(this IServiceCollection serviceCollection)
    {
        if (serviceCollection is null) throw new ArgumentNullException(nameof(serviceCollection));

        serviceCollection.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
    }
}