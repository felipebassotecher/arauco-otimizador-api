using Arauco.Otimizador.Common.Domain.Models.Conta;

namespace Arauco.Otimizador.Common.Domain.Services.Conta;

public interface IContaService
{
    Task<PerfilResponse> ObterPerfilAsync();
}