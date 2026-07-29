using Arauco.Otimizador.Common.Domain.Services.Cartao;
using Arauco.Otimizador.Common.Domain.Services.Cenario;
using Arauco.Otimizador.Common.Domain.Services.Demanda;
using Arauco.Otimizador.Common.Domain.Services.Parametro;
using Arauco.Otimizador.Data.Entities;
using Arauco.Otimizador.Data.MySql;
using Arauco.Otimizador.Service.CartaoService;
using Arauco.Otimizador.Service.CenarioService;
using Arauco.Otimizador.Service.DemandaService;
using Arauco.Otimizador.Service.ParametroService;
using Arauco.Otimizador.WebApi.Base.Builders;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
            s =>
            {
                // BD
                services.AddDbContext<DbContext>(options => options.UseMySqlLocal(Configuration));
                services.AddScoped<IUnitOfWork, UnitOfWork>();

                // Services
                s.AddScoped<ICartaoService, CartaoService>();
                s.AddScoped<ICenarioService, CenarioService>();
                s.AddScoped<IParametroService, ParametroService>();
                s.AddScoped<IDemandaService, DemandaService>();
            });
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        StartupBuilder.DefaultConfiguration(app, env);
    }
}