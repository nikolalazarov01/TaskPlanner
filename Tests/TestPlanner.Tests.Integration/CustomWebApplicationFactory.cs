using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using TaskPlanner.API.Data.Interfaces;
using TaskPlanner.API.Web;

namespace TestPlanner.Tests.Integration;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var userRepoDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IUserRepository));

            if (userRepoDescriptor != null)
            {
                services.Remove(userRepoDescriptor);
            }

            services.AddSingleton<IUserRepository, InMemoryUserRepository>();
        });
    }
}

