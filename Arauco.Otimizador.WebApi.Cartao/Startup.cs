using Arauco.Otimizador.Aws.Shared;
using Arauco.Otimizador.Common.Domain.Services.Cartao;
using Arauco.Otimizador.Data.Entities;
using Arauco.Otimizador.Data.MySql;
using Arauco.Otimizador.Service.CartaoService;
using Arauco.Otimizador.WebApi.Base.Builders;

namespace Arauco.Otimizador.WebApi;

public class Startup
{
    public Startup(IConfiguration configuration, IWebHostEnvironment env)
    {
        Configuration = configuration;
        Env = env;
    }

    public IConfiguration Configuration { get; }
    public IWebHostEnvironment Env { get; }

    // This method gets called by the runtime. Use this method to add services to the container
    public void ConfigureServices(IServiceCollection services)
    {
        StartupBuilder.DefaultServicesConfiguration(
            Configuration,
            Env,
            services,
            Cognitos.App,
            s =>
            {
                // BD
                services.AddDbContext<DbContext>();
                services.AddScoped<IUnitOfWork, UnitOfWork>();

                // Services
                s.AddScoped<ICartaoService, CartaoService>();
            });
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        StartupBuilder.DefaultConfiguration(app, env);
    }
}