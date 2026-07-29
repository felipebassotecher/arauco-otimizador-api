using Arauco.Otimizador.Common.Domain.Models.Colaborador;
using Arauco.Otimizador.Common.Domain.Services.Colaborador;
using Arauco.Otimizador.WebApi.Base.Controller;
using Microsoft.AspNetCore.Mvc;

namespace Arauco.Otimizador.WebApi.ColaboradorApi.Controllers;

[Route("")]
public class DefaultController : BaseController
{
    private readonly IColaboradorService colaboradorService;

    public DefaultController(IColaboradorService colaboradorService, IServiceProvider serviceProvider) : base(serviceProvider)
    {
        this.colaboradorService = colaboradorService;
    }

    [HttpGet("todos")]
    public async Task<List<ColaboradorListaModel>> ListarAsync()
    {
        return await colaboradorService.ListarAsync(await GetSessionAsync());
    }
}
