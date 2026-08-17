using Arauco.Otimizador.Common.Domain.Models.Cenario;
using Arauco.Otimizador.Common.Domain.Models.Contrato;
using Arauco.Otimizador.Common.Domain.Models.Criterio;
using Arauco.Otimizador.Common.Domain.Models.Otimizador;
using Arauco.Otimizador.Common.Domain.Models.Pedido;
using Arauco.Otimizador.Common.Domain.Services.Cenario;
using Arauco.Otimizador.Common.Domain.Services.Contrato;
using Arauco.Otimizador.Common.Domain.Services.Otimizador;
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
    private readonly IContratoService contratoService;

    public CenariosController(
        ICenarioService cenarioService,
        IOtimizadorService otimizadorService,
        IContratoService contratoService,
        IServiceProvider serviceProvider) : base(serviceProvider)
    {
        this.cenarioService = cenarioService;
        this.otimizadorService = otimizadorService;
        this.contratoService = contratoService;
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
    public async Task<ProcessarCenarioResponse> ProcessarAsync(string id)
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
        return await otimizadorService.OtimizarAsync(id, model);
    }

    [HttpGet("{id}/otimizar/semanas/{ano}/{semana}/pedidos")]
    public async Task<List<PedidoOtimizadoResponse>> ListarPedidosOtimizadosDaSemanaAsync(string id, int ano, int semana)
    {
        return await otimizadorService.ListarPedidosDaSemanaAsync(id, ano, semana);
    }

    [HttpGet("{id}/otimizar/nao-alocados")]
    public async Task<List<PedidoOtimizadoNaoAlocadoResponse>> ListarNaoAlocadosAsync(string id)
    {
        return await otimizadorService.ListarNaoAlocadosAsync(id);
    }

    [HttpPatch("{id}/otimizar/pedidos/mover")]
    public async Task<PedidoOtimizadoResponse> MoverPedidoOtimizadoAsync(string id, [FromBody] MoverPedidoOtimizadoRequest model)
    {
        return await otimizadorService.MoverPedidoAsync(id, model);
    }

    [HttpPatch("{id}/otimizar/pedidos/pin")]
    public async Task<PedidoOtimizadoResponse> AlternarPinPedidoOtimizadoAsync(string id, [FromBody] AlternarPinPedidoRequest model)
    {
        return await otimizadorService.AlternarPinAsync(id, model);
    }

    [HttpPost("{id}/enriquecer")]
    public async Task<List<ContratoResponse>> EnriquecerAsync(string id)
    {
        return await contratoService.EnriquecerAsync(id);
    }
}