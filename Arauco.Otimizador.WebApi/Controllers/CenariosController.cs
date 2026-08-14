using Arauco.Otimizador.Common.Domain.Models.Cenario;
using Arauco.Otimizador.Common.Domain.Models.Criterio;
using Arauco.Otimizador.Common.Domain.Models.Otimizador;
using Arauco.Otimizador.Common.Domain.Models.OtimizadorV2;
using Arauco.Otimizador.Common.Domain.Models.Pedido;
using Arauco.Otimizador.Common.Domain.Services.Cenario;
using Arauco.Otimizador.Common.Domain.Services.Otimizador;
using Arauco.Otimizador.Common.Domain.Services.OtimizadorV2;
using Arauco.Otimizador.WebApi.Base.Controller;
using Microsoft.AspNetCore.Mvc;
using Techer.Common.Domain.Exceptions;

namespace Arauco.Otimizador.WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class CenariosController : BaseController
{
    private readonly ICenarioService cenarioService;
    private readonly IOtimizadorService otimizadorService;
    private readonly IOtimizadorV2Service otimizadorV2Service;

    public CenariosController(
        ICenarioService cenarioService,
        IOtimizadorService otimizadorService,
        IOtimizadorV2Service otimizadorV2Service,
        IServiceProvider serviceProvider) : base(serviceProvider)
    {
        this.cenarioService = cenarioService;
        this.otimizadorService = otimizadorService;
        this.otimizadorV2Service = otimizadorV2Service;
    }

    // Registrado antes de [HttpGet("{id}")] para precedência de rota: segmento literal
    // "criterios-disponiveis" tem prioridade sobre o parâmetro "{id}" no attribute routing do
    // ASP.NET Core (changelog 2026-08-03). Assim /cenarios/criterios-disponiveis não cai em ObterAsync.
    [HttpGet("criterios-disponiveis")]
    public async Task<List<CriterioDisponivelResponse>> ListarCriteriosDisponiveisAsync()
    {
        return await cenarioService.ListarCriteriosDisponiveisAsync();
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

    [HttpPut("{id}")]
    public async Task<CenarioDetalheResponse> AtualizarAsync(string id, [FromBody] CenarioAtualizacaoRequest model)
    {
        return await cenarioService.AtualizarAsync(id, model);
    }

    [HttpDelete("{id}")]
    public async Task RemoverAsync(string id)
    {
        await cenarioService.RemoverAsync(id);
    }

    [HttpPost("{id}/csv")]
    public async Task<CenarioDetalheResponse> UploadArquivoAsync(string id, IFormFile arquivo)
    {
        if (arquivo == null || arquivo.Length == 0)
            throw new ApiException("Arquivo CSV não informado");

        await using var stream = arquivo.OpenReadStream();

        return await cenarioService.UploadArquivoAsync(id, arquivo.FileName, stream);
    }

    [HttpGet("{id}/csv")]
    public async Task<IActionResult> DownloadArquivoAsync(string id)
    {
        var (nome, conteudo) = await cenarioService.DownloadArquivoAsync(id);

        var bytes = System.Text.Encoding.UTF8.GetBytes(conteudo);

        return File(bytes, "text/csv", nome);
    }

    [HttpPost("{id}/processar")]
    public async Task<CenarioDetalheResponse> ProcessarAsync(string id)
    {
        return await cenarioService.ProcessarAsync(id);
    }

    [HttpGet("{id}/metricas")]
    public async Task<CenarioMetricasResponse> ObterMetricasAsync(string id)
    {
        return await cenarioService.ObterMetricasAsync(id);
    }

    [HttpGet("{id}/semanas/{ano}/{semana}/pedidos")]
    public async Task<List<PedidoResponse>> ListarPedidosDaSemanaAsync(string id, int ano, int semana)
    {
        return await cenarioService.ListarPedidosDaSemanaAsync(id, ano, semana);
    }

    [HttpPatch("{id}/pedidos/mover")]
    public async Task<PedidoResponse> MoverPedidoAsync(string id, [FromBody] MoverPedidoRequest model)
    {
        return await cenarioService.MoverPedidoAsync(id, model);
    }

    [HttpPost("{id}/submeter")]
    public async Task<CenarioDetalheResponse> SubmeterAsync(string id)
    {
        return await cenarioService.SubmeterAsync(id);
    }

    [HttpPost("{id}/otimizar")]
    public async Task<OtimizacaoResponse> OtimizarAsync(string id, [FromBody] OtimizacaoRequest? model)
    {
        return await otimizadorService.OtimizarCenarioAsync(id, model);
    }

    [HttpPost("{id}/otimizar-v2")]
    public async Task<OtimizacaoV2Response> OtimizarV2Async(string id, [FromBody] OtimizacaoV2Request? model)
    {
        return await otimizadorV2Service.OtimizarAsync(id, model);
    }

    [HttpGet("{id}/otimizar-v2/semanas/{ano}/{semana}/pedidos")]
    public async Task<List<PedidoV2Response>> ListarPedidosV2DaSemanaAsync(string id, int ano, int semana)
    {
        return await otimizadorV2Service.ListarPedidosDaSemanaAsync(id, ano, semana);
    }

    [HttpPatch("{id}/otimizar-v2/pedidos/mover")]
    public async Task<PedidoV2Response> MoverPedidoV2Async(string id, [FromBody] MoverPedidoV2Request model)
    {
        return await otimizadorV2Service.MoverPedidoAsync(id, model);
    }
}