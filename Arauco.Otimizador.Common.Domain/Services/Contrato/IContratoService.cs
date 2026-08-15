using Arauco.Otimizador.Common.Domain.Models.Contrato;

namespace Arauco.Otimizador.Common.Domain.Services.Contrato;

public interface IContratoService
{
    Task<List<ContratoResponse>> EnriquecerAsync(string cenarioId);
}
