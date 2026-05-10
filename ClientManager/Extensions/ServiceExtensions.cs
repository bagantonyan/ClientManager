using Asp.Versioning;
using ClientManager.Core.Domain.Entities;
using ClientManager.Core.Domain.Repositories;
using ClientManager.Core.Services;
using ClientManager.Core.Services.Abstractions;
using ClientManager.Infrastructure.Persistence;
using HealthChecks.UI.Client;
using LoggingService;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Reflection;
using System.Text;
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
                    RateLimitPartition.GetFixedWindowLimiter("GlobalLimiter",
                    partition => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 50,
                        QueueLimit = 2,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        Window = TimeSpan.FromMinutes(1)
                    }));

                opt.AddPolicy("SpecificPolicy", context =>
                    RateLimitPartition.GetFixedWindowLimiter("SpecificLimiter",
                    partition => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 10,
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

        public static void ConfigureIdentity(this IServiceCollection services)
        {
            var builder = services.AddIdentity<User, IdentityRole>(o =>
            {
                o.Password.RequireDigit = true;
                o.Password.RequireLowercase = false;
                o.Password.RequireUppercase = false;
                o.Password.RequireNonAlphanumeric = false;
                o.Password.RequiredLength = 10;
                o.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<RepositoryContext>()
            .AddDefaultTokenProviders();
        }

        public static void ConfigureJWT(this IServiceCollection services, IConfiguration configuration)
        {
            var jwtSettings = configuration.GetSection("JwtSettings");

            var secretKey = jwtSettings["Secret"]
                ?? throw new InvalidOperationException(
                    "JwtSettings:Secret is not configured. " +
                    "Set it locally with: dotnet user-secrets set \"JwtSettings:Secret\" \"<your-key>\" --project ClientManager. " +
                    "In production set the environment variable JwtSettings__Secret.");

            services.AddAuthentication(opt =>
            {
                opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings["validIssuer"],
                    ValidAudience = jwtSettings["validAudience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
                };
            });
        }
    }
}