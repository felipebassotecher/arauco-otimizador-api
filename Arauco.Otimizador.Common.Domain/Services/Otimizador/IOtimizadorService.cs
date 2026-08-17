using Arauco.Otimizador.Common.Domain.Models.Otimizador;

namespace Arauco.Otimizador.Common.Domain.Services.Otimizador;

public interface IOtimizadorService
{
    Task<OtimizacaoResponse> OtimizarAsync(string cenarioId, OtimizacaoRequest? request);
    Task<List<PedidoOtimizadoResponse>> ListarPedidosDaSemanaAsync(string cenarioId, int ano, int semana);
    Task<List<PedidoOtimizadoNaoAlocadoResponse>> ListarNaoAlocadosAsync(string cenarioId);
    Task<PedidoOtimizadoResponse> MoverPedidoAsync(string cenarioId, MoverPedidoOtimizadoRequest model);
    Task<PedidoOtimizadoResponse> AlternarPinAsync(string cenarioId, AlternarPinPedidoRequest model);
}
