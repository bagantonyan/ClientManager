using ClientManager.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ClientManager.IntegrationTests.Infrastructure
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = $"ClientManagerTests-{Guid.NewGuid():N}";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<DbContextOptions<RepositoryContext>>();
                services.RemoveAll<DbContextOptions>();

                var efServiceProvider = new ServiceCollection()
                    .AddEntityFrameworkInMemoryDatabase()
                    .BuildServiceProvider();

                services.AddDbContext<RepositoryContext>(opts => opts
                    .UseInMemoryDatabase(_databaseName)
                    .UseInternalServiceProvider(efServiceProvider)
                    .AddInterceptors(new RowVersionInterceptor()));
            });
        }
    }

    internal static class ServiceCollectionExtensions
    {
        public static IServiceCollection RemoveAll<T>(this IServiceCollection services)
        {
            foreach (var d in services.Where(d => d.ServiceType == typeof(T)).ToList())
                services.Remove(d);
            return services;
        }
    }
}
