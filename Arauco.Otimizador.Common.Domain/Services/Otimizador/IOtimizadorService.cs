using Arauco.Otimizador.Common.Domain.Models.Otimizador;

namespace Arauco.Otimizador.Common.Domain.Services.Otimizador;

public interface IOtimizadorService
{
    Task<OtimizacaoResponse> OtimizarCenarioAsync(string cenarioId, OtimizacaoRequest? request);
}
