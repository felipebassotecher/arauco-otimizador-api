using Arauco.Otimizador.Common.Domain.Interfaces;
using Arauco.Otimizador.Common.Domain.Session;
using Arauco.Otimizador.Data.Dynamo;
using Arauco.Otimizador.WebApi.Base.Config;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Serialization;
using Techer.Aws.Cognito.Models;
using Techer.Aws.Shared;
using Techer.Common.Domain.Interfaces;
using Techer.Common.Domain.Repositories;
using Techer.Common.WebApi.Util;

namespace Arauco.Otimizador.WebApi.Base.Builders
{
    public static class StartupBuilder
    {
        public static void DefaultServicesConfiguration(IConfiguration config, IWebHostEnvironment env, IServiceCollection services, AwsResource<CognitoData> pool, Action<IServiceCollection> custom)
        {
            DefaultServicesConfiguration(
                config,
                env,
                services,
                new List<AwsResource<CognitoData>> { pool },
                custom);
        }

        public static void DefaultServicesConfiguration(IConfiguration config, IWebHostEnvironment env, IServiceCollection services, List<AwsResource<CognitoData>> pools, Action<IServiceCollection> custom)
        {
            // Cors Config
            services.AddCors(o => o.AddPolicy("Default", builder =>
            {
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader();
            }));

            var auth = services
                .AddAuthentication(options =>
                {
                    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                });

            var environmentEnum = ApiEnvironmentVariables.GetEnvironmentEnum(env.EnvironmentName);
            var authSchemes = new List<string>();

            foreach (var pool in pools)
            {
                var cognito = pool.Get(environmentEnum);

                auth.AddJwtBearer("app-arauco-otimizador", options =>
                {
                    options.Audience = cognito.AppClientId;
                    options.Authority = cognito.Authority;
                    options.RequireHttpsMetadata = false;
                });

                authSchemes.Add($"app-arauco-otimizador");
            }

            if (authSchemes.Any())
            {
                services
                    .AddAuthorization(options =>
                    {
                        options.DefaultPolicy = new AuthorizationPolicyBuilder()
                            .RequireAuthenticatedUser()
                            .AddAuthenticationSchemes(authSchemes.ToArray())
                            .Build();
                    });
            }

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
            app.UseAuthentication();

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
