using ClientManager.Core.Services;
using ClientManager.Extensions;
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

            builder.Services.ConfigureCors();

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

            builder.Services.AddControllers(config =>
            {
                config.InputFormatters.Insert(0, GetJsonPatchInputFormatter());
            }).AddApplicationPart(typeof(ClientManager.Infrastructure.Presentation.AssemblyReference).Assembly);

            builder.Services.ConfigureSwagger();

            builder.Host.UseSerilog((hostContext, configuration) =>
            {
                configuration.ReadFrom.Configuration(hostContext.Configuration);
            });

            var app = builder.Build();

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

            app.UseStaticFiles();

            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.All
            });

            app.UseCors("CorsPolicy");

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }

        static NewtonsoftJsonPatchInputFormatter GetJsonPatchInputFormatter() =>
            new ServiceCollection().AddLogging().AddMvc().AddNewtonsoftJson()
            .Services.BuildServiceProvider()
            .GetRequiredService<IOptions<MvcOptions>>().Value.InputFormatters
            .OfType<NewtonsoftJsonPatchInputFormatter>().First();
    }
}
