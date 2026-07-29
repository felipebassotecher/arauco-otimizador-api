using Arauco.Otimizador.Data.Entities;
using Arauco.Otimizador.Data.Entities.Empresa;
using Arauco.Otimizador.Data.MySql;
using Arauco.Otimizador.WebApi.DataApi.Filter;
using Microsoft.AspNetCore.OData;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;

namespace Arauco.Otimizador.WebApi.DataApi;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddDbContext<HubDbContext>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddSingleton<ApiKeyAuthorizationFilter>();

        services.AddControllers()
            .AddNewtonsoftJson()
            .AddOData(opt => opt
                .Select()
                .Filter()
                .OrderBy()
                .SetMaxTop(100)
                .Count()
                .Expand()
                .AddRouteComponents("odata", GetEdmModel()));

        // Restricao nos logs
        services
            .AddLogging(builder =>
            {
                builder
                    .AddFilter("Microsoft", LogLevel.Information)
                    .AddFilter("System", LogLevel.Information)
                    .AddConsole();
            });
    }

    private IEdmModel GetEdmModel()
    {
        var modelBuilder = new ODataConventionModelBuilder();

        modelBuilder.EntitySet<Filial>("Filiais");
        modelBuilder.EntitySet<PostoTrabalho>("PostosTrabalho");

        return modelBuilder.GetEdmModel();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseHttpsRedirection();

        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }

}