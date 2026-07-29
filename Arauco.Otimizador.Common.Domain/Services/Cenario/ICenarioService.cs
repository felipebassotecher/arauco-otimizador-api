using Arauco.Otimizador.Common.Domain.Models.Cenario;
using Arauco.Otimizador.Common.Domain.Models.Pedido;

namespace Arauco.Otimizador.Common.Domain.Services.Cenario;

public interface ICenarioService
{
    Task<List<CenarioListaResponse>> ListarAsync();
    Task<CenarioDetalheResponse> ObterAsync(string cenarioId);
    Task<CenarioCriacaoResponse> CriarAsync(CenarioCriacaoRequest model);
    Task<CenarioUploadArquivoResponse> UploadArquivoAsync(string cenarioId, string nomeArquivo, Stream conteudo);
    Task RemoverAsync(string cenarioId);
    Task<CenarioProcessamentoResponse> ProcessarAsync(string cenarioId);
    Task<CenarioMetricasResponse> ObterMetricasAsync(string cenarioId);
    Task<List<PedidoListaResponse>> ListarPedidosDaSemanaAsync(string cenarioId, int ano, int semana);
    Task<PedidoMovimentacaoResponse> MoverPedidoAsync(string cenarioId, PedidoMovimentacaoRequest model);
    Task<CenarioSubmissaoResponse> SubmeterAsync(string cenarioId);
}
