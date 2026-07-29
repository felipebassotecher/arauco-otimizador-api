using Arauco.Otimizador.Common.Domain.Models.Colaborador;
using Arauco.Otimizador.Common.Domain.Models.Conta;
using Arauco.Otimizador.Common.Domain.Services.Colaborador;
using Arauco.Otimizador.Common.Domain.Session;
using Arauco.Otimizador.Data.Entities;
using Arauco.Otimizador.Service.Base;
using Microsoft.EntityFrameworkCore;
using Techer.Common.Domain.Exceptions;
using Techer.Common.Domain.Interfaces;

namespace Arauco.Otimizador.Service.ColaboradorService;

public class ColaboradorService : ServiceBase, IColaboradorService
{
    private readonly ISeniorUnitOfWork seniorUnitOfWork;

    public ColaboradorService(ISeniorUnitOfWork seniorUnitOfWork, IEnvironmentVariables environmentVariables) : base(null, environmentVariables)
    {
        this.seniorUnitOfWork = seniorUnitOfWork;
    }
    public async Task<List<ColaboradorListaModel>> ListarAsync(AppSessionModel session)
    {
        var colaboradores = await seniorUnitOfWork
            .ColaboradorRepository
            .AsQueryable()
            .Select(c => new ColaboradorListaModel
            {
                Key = c.ColaboradorId,
                Value = c.Nome
            })
            .ToListAsync();

        return colaboradores;
    }

    public async Task<ProfileModel> ObterPerfilAsync(AppSessionModel session)
    {
        var colaborador = await seniorUnitOfWork
            .ColaboradorRepository
            .Where(c => c.ColaboradorId == session.ColaboradorId && c.EmailComercial == session.Email)
            .Select(c => new ProfileModel
            {
                ColaboradorId = c.ColaboradorId,
                Nome = c.Nome
            })
            .FirstOrDefaultAsync() ?? throw new NotFoundException("Colaborador não encontrado");

        return colaborador;
    }
}
