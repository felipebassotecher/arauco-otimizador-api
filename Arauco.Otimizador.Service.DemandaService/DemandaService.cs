using Arauco.Otimizador.Common.Domain.Models.Demanda;
using Arauco.Otimizador.Common.Domain.Services.Demanda;
using Arauco.Otimizador.Common.Domain.Util;
using Arauco.Otimizador.Data.Entities;
using Arauco.Otimizador.Data.Entities.Demanda;
using Arauco.Otimizador.Service.Base;
using Microsoft.EntityFrameworkCore;
using Techer.Common.Domain.Exceptions;
using Techer.Common.Domain.Interfaces;
using Techer.Common.Id;

namespace Arauco.Otimizador.Service.DemandaService;

public class DemandaService : ServiceBase, IDemandaService
{
    public DemandaService(IUnitOfWork unitOfWork, IEnvironmentVariables environmentVariables) : base(unitOfWork, environmentVariables)
    {
    }

    public async Task<List<DemandaResponse>> ListarAsync(string cenarioId)
    {
        var demandas = await unitOfWork
            .DemandaRepository
            .Where(d => d.CenarioId == cenarioId)
            .ToListAsync();

        return demandas.Select(_Map).ToList();
    }

    public async Task<List<DemandaResponse>> UploadAsync(DemandaUploadRequest model)
    {
        if (!await unitOfWork.CenarioRepository.AnyAsync(c => c.CenarioId == model.CenarioId))
            throw new NotFoundException("Cenário não encontrado");

        var linhas = DemandaCsvParser.Parse(model.ConteudoCsv);

        if (linhas.Count == 0)
            throw new ApiException("Arquivo CSV inválido ou vazio");

        // Substitui as demandas existentes do cenário pelas novas (spec §2.3).
        var existentes = await unitOfWork.DemandaRepository.Where(d => d.CenarioId == model.CenarioId).ToListAsync();
        unitOfWork.DemandaRepository.RemoveRange(existentes);

        var demandas = new List<Demanda>();

        foreach (var linha in linhas)
        {
            demandas.Add(new Demanda
            {
                DemandaId = await IdGenerator.New(12),
                CenarioId = model.CenarioId,
                CarteiraId = linha.CarteiraId,
                Cliente = linha.Cliente,
                ClienteNome = linha.ClienteNome,
                Material = linha.Material,
                LinhaProdutoId = linha.LinhaProdutoId,
                Volume = linha.Volume,
                DataDocumento = linha.DataDocumento,
                DataEntregaDesejada = linha.DataEntrega,
                TipoFreteEnum = linha.TipoFrete,
                Segmento = linha.Segmento,
                CentroOriginal = linha.CentroOriginal
            });
        }

        unitOfWork.DemandaRepository.AddRange(demandas);

        await unitOfWork.SaveAsync();

        return demandas.Select(_Map).ToList();
    }

    private static DemandaResponse _Map(Demanda demanda)
    {
        return new DemandaResponse
        {
            Id = demanda.DemandaId,
            CarteiraId = demanda.CarteiraId,
            Cliente = demanda.Cliente,
            ClienteNome = demanda.ClienteNome,
            Material = demanda.Material,
            LinhaProdutoId = demanda.LinhaProdutoId,
            Volume = demanda.Volume,
            DataDocumento = demanda.DataDocumento,
            DataEntregaDesejada = demanda.DataEntregaDesejada,
            TipoFrete = demanda.TipoFreteEnum.ToString(),
            Segmento = demanda.Segmento,
            CentroOriginal = demanda.CentroOriginal
        };
    }
}