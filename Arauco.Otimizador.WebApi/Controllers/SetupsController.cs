using Arauco.Otimizador.Common.Domain.Models.Setup;
using Arauco.Otimizador.Common.Domain.Services.Setup;
using Arauco.Otimizador.WebApi.Base.Controller;
using Microsoft.AspNetCore.Mvc;

namespace Arauco.Otimizador.WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class SetupsController : BaseController
{
    private readonly ISetupService setupService;

    public SetupsController(ISetupService setupService, IServiceProvider serviceProvider) : base(serviceProvider)
    {
        this.setupService = setupService;
    }

    [HttpGet("")]
    public async Task<List<SetupListaResponse>> ListarAsync()
    {
        return await setupService.ListarAsync();
    }

    [HttpGet("{id}")]
    public async Task<SetupDetalheResponse> ObterAsync(string id)
    {
        return await setupService.ObterAsync(id);
    }

    [HttpPost("")]
    public async Task<SetupCriacaoResponse> CriarAsync([FromBody] SetupCriacaoRequest model)
    {
        return await setupService.CriarAsync(model);
    }

    [HttpPut("{id}")]
    public async Task<SetupDetalheResponse> AtualizarAsync(string id, [FromBody] SetupAtualizacaoRequest model)
    {
        return await setupService.AtualizarAsync(id, model);
    }

    [HttpPost("{id}/clonar")]
    public async Task<SetupDetalheResponse> ClonarAsync(string id)
    {
        return await setupService.ClonarAsync(id);
    }

    [HttpDelete("{id}")]
    public async Task RemoverAsync(string id)
    {
        await setupService.RemoverAsync(id);
    }
}
