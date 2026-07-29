using Arauco.Otimizador.Common.Domain.Models.Parametro;

namespace Arauco.Otimizador.Common.Domain.Services.Parametro;

public interface IParametroService
{
    Task<List<ParametroListaResponse>> ListarAsync();
    Task<List<ParametroListaResponse>> ListarAtivosAsync();
    Task<ParametroDetalheResponse> ObterAsync(string parametroId);
    Task<ParametroCriacaoResponse> CriarAsync(ParametroCriacaoRequest model);
    Task<ParametroAtualizacaoResponse> AtualizarAsync(string parametroId, ParametroAtualizacaoRequest model);
    Task RemoverAsync(string parametroId);
}
