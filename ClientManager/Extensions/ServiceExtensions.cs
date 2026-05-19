using Asp.Versioning;
using ClientManager.Core.Domain.Repositories;
using ClientManager.Core.Services;
using ClientManager.Core.Services.Abstractions;
using ClientManager.Infrastructure.Persistence;
using ClientManager.Middleware;
using HealthChecks.UI.Client;
using LoggingService;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Reflection;
using System.Threading.RateLimiting;

namespace ClientManager.Extensions
{
    public static class ServiceExtensions
    {
        public static void ConfigureCors(this IServiceCollection services, IConfiguration configuration, IHostEnvironment env) =>
            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy", builder =>
                {
                    if (env.IsDevelopment())
                    {
                        builder.AllowAnyOrigin()
                               .AllowAnyMethod()
                               .AllowAnyHeader()
                               .WithExposedHeaders("X-Pagination");
                    }
                    else
                    {
                        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                                      ?? Array.Empty<string>();
                        builder.WithOrigins(origins)
                               .AllowAnyMethod()
                               .AllowAnyHeader()
                               .WithExposedHeaders("X-Pagination");
                    }
                });
            });

        public static void ConfigureLoggerService(this IServiceCollection services) =>
            services.AddSingleton<ILoggerManager, LoggerManager>();

        public static void ConfigureTimeProvider(this IServiceCollection services) =>
            services.AddSingleton(TimeProvider.System);

        public static void ConfigureRepositoryManager(this IServiceCollection services) =>
            services.AddScoped<IRepositoryManager, RepositoryManager>();

        public static void ConfigureServiceManager(this IServiceCollection services) =>
            services.AddScoped<IServiceManager, ServiceManager>();

        public static void ConfigureSqlContext(this IServiceCollection services, IConfiguration configuration) =>
            services.AddDbContext<RepositoryContext>(opts =>
                opts.UseSqlServer(configuration.GetConnectionString("sqlConnection")));

        public static void ConfigureSwagger(this IServiceCollection services)
        {
            services.AddSwaggerGen(s =>
            {
                s.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "ClientManager API",
                    Version = "v1",
                    Description = "ClientManager API working with clients and founders",
                    Contact = new OpenApiContact
                    {
                        Name = "Bagrat Antonyan",
                        Email = "bagrat.antonyan.work@mail.ru"
                    }
                });

                var xmlFile = $"{typeof(Infrastructure.Presentation.AssemblyReference)
                .Assembly.GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                s.IncludeXmlComments(xmlPath);
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
                    RateLimitPartition.GetSlidingWindowLimiter(
                        GetClientIp(context),
                        _ => new SlidingWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = 5,
                            QueueLimit = 2,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            Window = TimeSpan.FromMinutes(1),
                            SegmentsPerWindow = 6
                        }));

                opt.AddPolicy("SpecificPolicy", context =>
                    RateLimitPartition.GetSlidingWindowLimiter(
                        GetClientIp(context),
                        _ => new SlidingWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = 10,
                            Window = TimeSpan.FromSeconds(10),
                            SegmentsPerWindow = 5
                        }));

                opt.OnRejected = async (context, token) =>
                {
                    var http = context.HttpContext;
                    http.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                    int? retryAfterSeconds = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
                        ? (int)Math.Ceiling(retryAfter.TotalSeconds)
                        : null;

                    if (retryAfterSeconds is int seconds)
                        http.Response.Headers.RetryAfter = seconds.ToString();

                    var clientIp = GetClientIp(http);
                    var policyName = http.GetEndpoint()?.Metadata
                        .GetMetadata<EnableRateLimitingAttribute>()?.PolicyName ?? "Global";

                    var loggerFactory = http.RequestServices.GetRequiredService<ILoggerFactory>();
                    var logger = loggerFactory.CreateLogger("RateLimiter");

                    logger.LogWarning(
                        "Rate limit rejected. ClientIp={ClientIp} Policy={Policy} Method={Method} Path={Path} RetryAfterSec={RetryAfterSec}",
                        clientIp, policyName, http.Request.Method, http.Request.Path.Value, retryAfterSeconds);

                    var correlationId = http.Response.Headers
                        .TryGetValue(CorrelationIdMiddleware.HeaderName, out var v) ? v.ToString() : null;

                    var problem = new ProblemDetails
                    {
                        Title = "Too many requests",
                        Status = StatusCodes.Status429TooManyRequests,
                        Detail = retryAfterSeconds is int s
                            ? $"Rate limit exceeded. Try again in {s} second(s)."
                            : "Rate limit exceeded. Try again later.",
                        Type = "RateLimitExceeded",
                        Extensions = { ["correlationId"] = correlationId }
                    };

                    var problemService = http.RequestServices.GetRequiredService<IProblemDetailsService>();
                    var written = await problemService.TryWriteAsync(new ProblemDetailsContext
                    {
                        HttpContext = http,
                        ProblemDetails = problem
                    });

                    if (!written)
                        await http.Response.WriteAsJsonAsync(problem, token);
                };
            });
        }

        private static string GetClientIp(HttpContext context) =>
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

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

        public static void ConfigureOpenTelemetry(this IServiceCollection services, IConfiguration configuration, IHostEnvironment env)
        {
            var serviceName = Assembly.GetEntryAssembly()?.GetName().Name ?? "ClientManager";
            var serviceVersion = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "1.0.0";

            services.AddOpenTelemetry()
                .ConfigureResource(rb => rb.AddService(serviceName, serviceVersion: serviceVersion))
                .WithTracing(tb =>
                {
                    tb.AddAspNetCoreInstrumentation(opt =>
                      {
                          opt.Filter = ctx => !ctx.Request.Path.StartsWithSegments("/health");
                      })
                      .AddHttpClientInstrumentation()
                      .AddSqlClientInstrumentation();

                    if (env.IsDevelopment())
                        tb.AddConsoleExporter();

                    var otlpEndpoint = configuration["OpenTelemetry:OtlpEndpoint"];
                    if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                        tb.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
                });
        }

        public static WebApplication MigrateDatabase(this WebApplication app)
        {
            using (var scope = app.Services.CreateScope())
            {
                using (var dbContext = scope.ServiceProvider.GetRequiredService<RepositoryContext>())
                {
                    dbContext.Database.Migrate();
                }
            }

            return app;
        }

    }
}