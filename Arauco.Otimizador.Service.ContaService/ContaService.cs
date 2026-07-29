using Arauco.Otimizador.Common.Domain.Session;
using Arauco.Otimizador.Data.Entities;
using Arauco.Otimizador.Service.Base;
using Microsoft.EntityFrameworkCore;
using Techer.Common.Domain.Exceptions;
using Techer.Common.Domain.Interfaces;

namespace Arauco.Otimizador.Service.ContaService;

public class ContaService : ServiceBase
{
    private readonly ISeniorUnitOfWork seniorUnitOfWork;

    public ContaService(ISeniorUnitOfWork seniorUnitOfWork, IEnvironmentVariables env) : base(null, env)
    {
        this.seniorUnitOfWork = seniorUnitOfWork;
    }

    public async Task<AppSessionModel> CriarSessaoAsync(int colaboradorId)
    {
        var usuario = await seniorUnitOfWork
            .ColaboradorRepository
            .Where(u => u.ColaboradorId == colaboradorId && u.Ativo)
            .Select(u => new
            {
                u.Nome,
                u.EmailComercial
            }).FirstOrDefaultAsync();

        if (usuario == null)
            throw new ApiException("Colaborador não encontrado.");

        var sessao = new AppSessionModel
        {
            ColaboradorId = colaboradorId,
            SessionId = Guid.NewGuid().ToString(),
            Nome = usuario.Nome,
            Email = usuario.EmailComercial
        };

        return sessao;
    }
}
