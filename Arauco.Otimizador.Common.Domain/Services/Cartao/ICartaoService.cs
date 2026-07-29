using Arauco.Otimizador.Common.Domain.Models.Cartao;
using Arauco.Otimizador.Common.Domain.Session;

namespace Arauco.Otimizador.Common.Domain.Services.Cartao;

public interface ICartaoService
{
    public Task<List<CartaoListaModel>> ListarAsync(AppSessionModel session);
    public Task<CartaoDetalheModel> ObterAsync(string cartaoId, AppSessionModel session);
    public Task<string> NovoAsync(CartaoNovoModel model, AppSessionModel session);
}
