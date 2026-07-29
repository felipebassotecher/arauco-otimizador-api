using Arauco.Otimizador.Common.Domain.Models.Demanda;

namespace Arauco.Otimizador.Common.Domain.Services.Demanda;

public interface IDemandaService
{
    Task<List<DemandaListaResponse>> ListarAsync(string cenarioId);
    Task<List<DemandaUploadResponse>> UploadAsync(DemandaUploadRequest model);
}
