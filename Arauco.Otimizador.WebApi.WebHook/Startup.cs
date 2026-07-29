using Arauco.Otimizador.Data.Dynamo;
using Arauco.Otimizador.WebApi.Flow.Flow.Refeicao;
using Arauco.Otimizador.WebApi.Flow.Util;
using Newtonsoft.Json.Serialization;
using Techer.Common.Domain.Repositories;

namespace Arauco.Otimizador.WebApi.Flow;

public class Startup
{
    public static IConfiguration Configuration { get; private set; }
    public static IWebHostEnvironment HostingEnvironment { get; set; }

    public Startup(IConfiguration configuration, IWebHostEnvironment hostingEnvironment)
    {
        Configuration = configuration;
        HostingEnvironment = hostingEnvironment;
    }

    // This method gets called by the runtime. Use this method to add services to the container
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<CryptoService>();

        //services.AddScoped<IEnvironmentVariables, ApiEnvironmentVariables>();

        // Repositories
        services.AddScoped<IFlowRepository, FlowRepository>();
        services.AddScoped<IKeyValueRepository, KeyValueRepository>();

        // Flow
        services.AddScoped<RefeicaoFlowHandler>();

        services
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
        //app.UseExceptionHandler(builder => ErrorBuilder.Generate(builder));
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