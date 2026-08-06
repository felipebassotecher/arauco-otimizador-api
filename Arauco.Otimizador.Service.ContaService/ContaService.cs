using Arauco.Otimizador.Common.Domain.Models.Conta;
using Arauco.Otimizador.Common.Domain.Services.Conta;
using Arauco.Otimizador.Data.Entities;
using Arauco.Otimizador.Service.Base;
using Techer.Common.Domain.Interfaces;

namespace Arauco.Otimizador.Service.ContaService;

public class ContaService : ServiceBase, IContaService
{
    public ContaService(IUnitOfWork unitOfWork, IEnvironmentVariables environmentVariables) : base(unitOfWork, environmentVariables)
    {
    }

    // STUB: sem autenticação JWT/Cognito na API (decisão do projeto), não há usuário autenticado
    // real. Retorna um perfil fixo para destravar o front (GET /conta/profile). Quando a autenticação
    // for reintroduzida, derivar colaboradorId/nome/email do usuário logado (claims do token / DB).
    public Task<PerfilResponse> ObterPerfilAsync()
    {
        return Task.FromResult(new PerfilResponse
        {
            ColaboradorId = "STUB01",
            Nome = "Usuário (stub)",
            Email = "usuario.stub@arauco.com"
        });
    }
}