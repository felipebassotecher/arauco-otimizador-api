using Arauco.Otimizador.Common.Domain.Models.Conta;
using Arauco.Otimizador.Common.Domain.Services.Conta;
using Arauco.Otimizador.WebApi.Base.Controller;
using Microsoft.AspNetCore.Mvc;

namespace Arauco.Otimizador.WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class ContaController : BaseController
{
    private readonly IContaService contaService;

    public ContaController(IContaService contaService, IServiceProvider serviceProvider) : base(serviceProvider)
    {
        this.contaService = contaService;
    }

    [HttpGet("profile")]
    public async Task<PerfilResponse> ObterPerfilAsync()
    {
        return await contaService.ObterPerfilAsync();
    }
}