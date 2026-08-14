using Arauco.Otimizador.Common.Domain.Models.OtimizadorV2;

namespace Arauco.Otimizador.Common.Domain.Services.OtimizadorV2;

public interface IOtimizadorV2Service
{
    Task<OtimizacaoV2Response> OtimizarAsync(string cenarioId, OtimizacaoV2Request? request);
    Task<List<PedidoV2Response>> ListarPedidosDaSemanaAsync(string cenarioId, int ano, int semana);
    Task<PedidoV2Response> MoverPedidoAsync(string cenarioId, MoverPedidoV2Request model);
}
