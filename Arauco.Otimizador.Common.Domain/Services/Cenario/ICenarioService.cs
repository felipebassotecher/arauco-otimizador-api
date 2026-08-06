using Arauco.Otimizador.Common.Domain.Models.Cenario;
using Arauco.Otimizador.Common.Domain.Models.Criterio;
using Arauco.Otimizador.Common.Domain.Models.Pedido;

namespace Arauco.Otimizador.Common.Domain.Services.Cenario;

public interface ICenarioService
{
    Task<List<CriterioDisponivelResponse>> ListarCriteriosDisponiveisAsync();
    Task<List<CenarioListaResponse>> ListarAsync();
    Task<CenarioDetalheResponse> ObterAsync(string cenarioId);
    Task<CenarioCriacaoResponse> CriarAsync(CenarioCriacaoRequest model);
    Task<CenarioDetalheResponse> AtualizarAsync(string cenarioId, CenarioAtualizacaoRequest model);
    Task<CenarioDetalheResponse> UploadArquivoAsync(string cenarioId, string nomeArquivo, Stream conteudo);
    Task<(string Nome, string Conteudo)> DownloadArquivoAsync(string cenarioId);
    Task RemoverAsync(string cenarioId);
    Task<CenarioDetalheResponse> ProcessarAsync(string cenarioId);
    Task<CenarioMetricasResponse> ObterMetricasAsync(string cenarioId);
    Task<List<PedidoResponse>> ListarPedidosDaSemanaAsync(string cenarioId, int ano, int semana);
    Task<PedidoResponse> MoverPedidoAsync(string cenarioId, MoverPedidoRequest model);
    Task<CenarioDetalheResponse> SubmeterAsync(string cenarioId);
}