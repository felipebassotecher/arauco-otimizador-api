using Arauco.Otimizador.Common.Domain.Models.Parametro;
using Arauco.Otimizador.Common.Domain.Services.Parametro;
using Arauco.Otimizador.WebApi.Base.Controller;
using Microsoft.AspNetCore.Mvc;

namespace Arauco.Otimizador.WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class ParametrosController : BaseController
{
    private readonly IParametroService parametroService;

    public ParametrosController(IParametroService parametroService, IServiceProvider serviceProvider) : base(serviceProvider)
    {
        this.parametroService = parametroService;
    }

    [HttpGet("")]
    public async Task<List<ParametroListaResponse>> ListarAsync()
    {
        return await parametroService.ListarAsync();
    }

    [HttpGet("ativos")]
    public async Task<List<ParametroListaResponse>> ListarAtivosAsync()
    {
        return await parametroService.ListarAtivosAsync();
    }

    [HttpGet("{id}")]
    public async Task<ParametroDetalheResponse> ObterAsync(string id)
    {
        return await parametroService.ObterAsync(id);
    }

    [HttpPost("")]
    public async Task<ParametroCriacaoResponse> CriarAsync([FromBody] ParametroCriacaoRequest model)
    {
        return await parametroService.CriarAsync(model);
    }

    [HttpPut("{id}")]
    public async Task<ParametroAtualizacaoResponse> AtualizarAsync(string id, [FromBody] ParametroAtualizacaoRequest model)
    {
        return await parametroService.AtualizarAsync(id, model);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> RemoverAsync(string id)
    {
        await parametroService.RemoverAsync(id);

        return NoContent();
    }
}
