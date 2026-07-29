using Arauco.Otimizador.Common.Domain.Interfaces;
using Arauco.Otimizador.Common.Domain.Session;
using Arauco.Otimizador.Data.Dynamo;
using Arauco.Otimizador.WebApi.Base.Config;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Serialization;
using Techer.Common.Domain.Interfaces;
using Techer.Common.Domain.Repositories;
using Techer.Common.WebApi.Util;

namespace Arauco.Otimizador.WebApi.Base.Builders
{
    public static class StartupBuilder
    {
        public static void DefaultServicesConfiguration(IConfiguration config, IWebHostEnvironment env, IServiceCollection services, Action<IServiceCollection> custom)
        {
            // Cors Config
            services.AddCors(o => o.AddPolicy("Default", builder =>
            {
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader();
            }));

            // Default Services
            services.AddScoped<IEnvironmentVariables, ApiEnvironmentVariables>();
            services.AddScoped<ISessionManager<AppSessionModel>, AppSessionManager>();
            services.AddScoped<IUserIdentity, AppUserIdentity>();

            // Default Repositories
            services.AddScoped<IKeyValueRepository, KeyValueRepository>();
            services.AddScoped<ILogRepository, LogRepository>();

            // Custom config
            custom(services);

            services
                .AddHttpContextAccessor()
                .AddControllers()
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                    options.SerializerSettings.DateTimeZoneHandling = Newtonsoft.Json.DateTimeZoneHandling.Utc;
                });
        }

        public static void DefaultConfiguration(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseCors("Default");
            app.UseExceptionHandler(builder => ErrorBuilder.Generate(builder));

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
