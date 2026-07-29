using Arauco.Otimizador.Common.Domain.Models.Colaborador;
using Arauco.Otimizador.Common.Domain.Models.Conta;
using Arauco.Otimizador.Common.Domain.Session;

namespace Arauco.Otimizador.Common.Domain.Services.Colaborador;

public interface IColaboradorService
{
    public Task<List<ColaboradorListaModel>> ListarAsync(AppSessionModel session);
    public Task<ProfileModel> ObterPerfilAsync(AppSessionModel session);
}
