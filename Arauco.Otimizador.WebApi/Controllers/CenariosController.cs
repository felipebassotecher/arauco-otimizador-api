using Arauco.Otimizador.Common.Domain.Models.Cenario;
using Arauco.Otimizador.Common.Domain.Models.Pedido;
using Arauco.Otimizador.Common.Domain.Services.Cenario;
using Arauco.Otimizador.WebApi.Base.Controller;
using Microsoft.AspNetCore.Mvc;
using Techer.Common.Domain.Exceptions;

namespace Arauco.Otimizador.WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class CenariosController : BaseController
{
    private readonly ICenarioService cenarioService;

    public CenariosController(ICenarioService cenarioService, IServiceProvider serviceProvider) : base(serviceProvider)
    {
        this.cenarioService = cenarioService;
    }

    [HttpGet("")]
    public async Task<List<CenarioListaResponse>> ListarAsync()
    {
        return await cenarioService.ListarAsync();
    }

    [HttpGet("{id}")]
    public async Task<CenarioDetalheResponse> ObterAsync(string id)
    {
        return await cenarioService.ObterAsync(id);
    }

    [HttpPost("")]
    public async Task<CenarioCriacaoResponse> CriarAsync([FromBody] CenarioCriacaoRequest model)
    {
        return await cenarioService.CriarAsync(model);
    }

    [HttpPost("{id}/csv")]
    public async Task<CenarioUploadArquivoResponse> UploadArquivoAsync(string id, IFormFile arquivo)
    {
        if (arquivo == null || arquivo.Length == 0)
            throw new ApiException("Arquivo CSV não informado");

        await using var stream = arquivo.OpenReadStream();

        return await cenarioService.UploadArquivoAsync(id, arquivo.FileName, stream);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> RemoverAsync(string id)
    {
        await cenarioService.RemoverAsync(id);

        return NoContent();
    }

    [HttpPost("{id}/processar")]
    public async Task<CenarioProcessamentoResponse> ProcessarAsync(string id)
    {
        return await cenarioService.ProcessarAsync(id);
    }

    [HttpGet("{id}/metricas")]
    public async Task<CenarioMetricasResponse> ObterMetricasAsync(string id)
    {
        return await cenarioService.ObterMetricasAsync(id);
    }

    [HttpGet("{id}/semanas/{ano}/{semana}/pedidos")]
    public async Task<List<PedidoListaResponse>> ListarPedidosDaSemanaAsync(string id, int ano, int semana)
    {
        return await cenarioService.ListarPedidosDaSemanaAsync(id, ano, semana);
    }

    [HttpPatch("{id}/pedidos/mover")]
    public async Task<PedidoMovimentacaoResponse> MoverPedidoAsync(string id, [FromBody] PedidoMovimentacaoRequest model)
    {
        return await cenarioService.MoverPedidoAsync(id, model);
    }

    [HttpPost("{id}/submeter")]
    public async Task<CenarioSubmissaoResponse> SubmeterAsync(string id)
    {
        return await cenarioService.SubmeterAsync(id);
    }
}
