using ClientManager.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ClientManager.IntegrationTests.Infrastructure
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = $"ClientManagerTests-{Guid.NewGuid():N}";

        static CustomWebApplicationFactory()
        {
            Environment.SetEnvironmentVariable(
                "JwtSettings__Secret",
                "test-secret-key-for-integration-tests-only-32+chars");
        }

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

                services.Configure<AuthenticationOptions>(opts =>
                {
                    opts.DefaultScheme             = TestAuthHandler.SchemeName;
                    opts.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    opts.DefaultChallengeScheme    = TestAuthHandler.SchemeName;
                });
                services.AddAuthentication(TestAuthHandler.SchemeName)
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                        TestAuthHandler.SchemeName,
                        _ => { });
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