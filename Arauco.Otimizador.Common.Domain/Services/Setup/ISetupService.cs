using Arauco.Otimizador.Common.Domain.Models.Setup;

namespace Arauco.Otimizador.Common.Domain.Services.Setup;

public interface ISetupService
{
    Task<List<SetupListaResponse>> ListarAsync();
    Task<SetupDetalheResponse> ObterAsync(string setupId);
    Task<SetupCriacaoResponse> CriarAsync(SetupCriacaoRequest model);
    Task<SetupDetalheResponse> AtualizarAsync(string setupId, SetupAtualizacaoRequest model);
}
