using Arauco.Otimizador.Common.Domain.Models;
using Arauco.Otimizador.Common.Domain.Services.Auth;
using Arauco.Otimizador.Data.Entities;
using Arauco.Otimizador.Data.MySql;
using Arauco.Otimizador.Service.AuthService;
using Newtonsoft.Json.Serialization;
using Techer.Common.Domain.Interfaces;
using Techer.Common.WebApi.Util;

namespace Arauco.Otimizador.WebApi.AuthApi;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container
    public void ConfigureServices(IServiceCollection services)
    {
        // Cors Config
        services.AddCors(o => o.AddPolicy("Default", builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        }));

        // Settings
        var entraConfiguration = Configuration.GetSection("EntraConfiguration");
        services.Configure<EntraConfigurationModel>(entraConfiguration);

        // Default Services
        services.AddScoped<IEnvironmentVariables, ApiEnvironmentVariables>();
        services.AddScoped<IEntraService, EntraService>();
        services.AddScoped<IAuthService, AuthService>();

        // BD
        services.AddDbContext<SeniorDbContext>();
        services.AddScoped<ISeniorUnitOfWork, SeniorUnitOfWork>();

        services
            .AddHttpContextAccessor()
            .AddControllers()
            .AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                options.SerializerSettings.DateTimeZoneHandling = Newtonsoft.Json.DateTimeZoneHandling.Utc;
            });

    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseCors("Default");

        //if (env.IsDevelopment())
        //{
            app.UseDeveloperExceptionPage();
        //}

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