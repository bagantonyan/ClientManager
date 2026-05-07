using ClientManager.Core.Services;
using ClientManager.Extensions;
using ClientManager.Infrastructure.Presentation.Validators.Clients;
using FluentValidation;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Options;
using Serilog;

namespace ClientManager
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.ConfigureCors(builder.Configuration, builder.Environment);

            builder.Services.ConfigureLoggerService();

            builder.Services.ConfigureRepositoryManager();

            builder.Services.ConfigureServiceManager();

            builder.Services.ConfigureSqlContext(builder.Configuration);

            builder.Services.AddAutoMapper(cfg => { }, typeof(MappingProfile));

            builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

            builder.Services.AddProblemDetails();

            builder.Services.Configure<ApiBehaviorOptions>(options =>
            {
                options.SuppressModelStateInvalidFilter = true;
            });

            builder.Services.ConfigureVersioning();

            builder.Services.ConfigureRateLimitingOptions();

            builder.Services.ConfigureHealthChecks(builder.Configuration);

            builder.Services.AddControllers(config =>
            {
                config.InputFormatters.Insert(0, GetJsonPatchInputFormatter());
            }).AddApplicationPart(typeof(ClientManager.Infrastructure.Presentation.AssemblyReference).Assembly);

            builder.Services.AddValidatorsFromAssemblyContaining(typeof(ClientForCreationDtoValidator));

            builder.Services.ConfigureSwagger();

            builder.Host.UseSerilog((hostContext, configuration) =>
            {
                configuration.ReadFrom.Configuration(hostContext.Configuration);
            });

            var app = builder.Build();

            app.Lifetime.ApplicationStarted.Register(() =>
                Log.Information("Application started. Environment: {Environment}.", app.Environment.EnvironmentName));
            app.Lifetime.ApplicationStopping.Register(() =>
                Log.Information("Application is stopping..."));
            app.Lifetime.ApplicationStopped.Register(() =>
                Log.Information("Application stopped."));

            app.UseSerilogRequestLogging();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "ClientManager API v1");
                });
            }
            else
                app.UseHsts();

            app.UseExceptionHandler(opt => { });

            app.UseHttpsRedirection();

            app.ConfigureHealthChecksEndpoints();

            app.UseStaticFiles();

            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.All
            });

            app.UseRateLimiter();

            app.UseCors("CorsPolicy");

            if (app.Environment.IsDevelopment())
                app.MigrateDatabase();

            app.MapControllers();

            try
            {
                Log.Information("Starting ClientManager web host...");
                app.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "ClientManager web host terminated unexpectedly.");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        static NewtonsoftJsonPatchInputFormatter GetJsonPatchInputFormatter() =>
            new ServiceCollection().AddLogging().AddMvc().AddNewtonsoftJson()
            .Services.BuildServiceProvider()
            .GetRequiredService<IOptions<MvcOptions>>().Value.InputFormatters
            .OfType<NewtonsoftJsonPatchInputFormatter>().First();
    }
}
