using Arauco.Otimizador.Common.Domain.Models.Conta;
using Arauco.Otimizador.Common.Domain.Services.Colaborador;
using Arauco.Otimizador.WebApi.Base.Controller;
using Microsoft.AspNetCore.Mvc;

namespace Arauco.Otimizador.WebApi.Api.Controllers;

[Route("")]
public class DefaultController : BaseController
{
    private readonly IColaboradorService colaboradorService;

    public DefaultController(IColaboradorService colaboradorService, IServiceProvider serviceProvider) : base(serviceProvider)
    {
        this.colaboradorService = colaboradorService;
    }

    [HttpGet("profile")]
    public async Task<ProfileModel> ObterPerfilAsync()
    {
        return await colaboradorService.ObterPerfilAsync(await GetSessionAsync());
    }
}
