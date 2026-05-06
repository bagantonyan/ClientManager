using Asp.Versioning;
using ClientManager.Core.Domain.Repositories;
using ClientManager.Core.Services;
using ClientManager.Core.Services.Abstractions;
using ClientManager.Infrastructure.Persistence;
using HealthChecks.UI.Client;
using LoggingService;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;

namespace ClientManager.Extensions
{
    public static class ServiceExtensions
    {
        public static void ConfigureCors(this IServiceCollection services) =>
            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy", builder =>
                    builder.AllowAnyOrigin()
                           .AllowAnyMethod()
                           .AllowAnyHeader()
                           .WithExposedHeaders("X-Pagination"));
            });

        public static void ConfigureLoggerService(this IServiceCollection services) =>
            services.AddSingleton<ILoggerManager, LoggerManager>();

        public static void ConfigureRepositoryManager(this IServiceCollection services) =>
            services.AddScoped<IRepositoryManager, RepositoryManager>();

        public static void ConfigureServiceManager(this IServiceCollection services) =>
            services.AddScoped<IServiceManager, ServiceManager>();

        public static void ConfigureSqlContext(this IServiceCollection services, IConfiguration configuration) =>
            services.AddDbContext<RepositoryContext>(opts =>
                opts.UseSqlServer(configuration.GetConnectionString("sqlConnection")));

        public static void ConfigureSwagger(this IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new() { Title = "ClientManager API", Version = "v1" });
            });
        }

        public static void ConfigureVersioning(this IServiceCollection services)
        {
            services.AddApiVersioning(opt =>
            {
                opt.ReportApiVersions = true;
                opt.AssumeDefaultVersionWhenUnspecified = true;
                opt.DefaultApiVersion = new ApiVersion(1, 0);
                opt.ApiVersionReader = new HeaderApiVersionReader("api-version");
            }).AddMvc();
        }

        public static void ConfigureRateLimitingOptions(this IServiceCollection services)
        {
            services.AddRateLimiter(opt =>
            {
                opt.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                    RateLimitPartition.GetFixedWindowLimiter("GlobalLimiter",
                    partition => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 5,
                        QueueLimit = 2,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        Window = TimeSpan.FromMinutes(1)
                    }));

                opt.AddPolicy("SpecificPolicy", context =>
                    RateLimitPartition.GetFixedWindowLimiter("SpecificLimiter",
                    partition => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 3,
                        Window = TimeSpan.FromSeconds(10)
                    }));

                opt.OnRejected = async (context, token) =>
                {
                    context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                        await context.HttpContext.Response
                            .WriteAsync($"Too many requests. " +
                            $"Please try again after {retryAfter.TotalSeconds} second(s).", token);
                    else
                        await context.HttpContext.Response
                            .WriteAsync("Too many requests. Please try again later.", token);
                };
            });
        }

        public static void ConfigureHealthChecks(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHealthChecks()
                .AddSqlServer(configuration.GetConnectionString("sqlConnection")!, name: "Sql Health");

            services.AddHealthChecksUI()
                .AddInMemoryStorage();
        }

        public static void ConfigureHealthChecksEndpoints(this WebApplication app)
        {
            app.MapHealthChecks("/health", new HealthCheckOptions
            {
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });

            app.MapHealthChecksUI();
        }
    }
}