using Arauco.Otimizador.Common.Domain.Models.Demanda;

namespace Arauco.Otimizador.Common.Domain.Services.Demanda;

public interface IDemandaService
{
    Task<List<DemandaResponse>> ListarAsync(string cenarioId);
    Task<List<DemandaResponse>> UploadAsync(DemandaUploadRequest model);
}