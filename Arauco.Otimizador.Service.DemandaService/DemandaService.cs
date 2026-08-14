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
                DemandaId = await IdGenerator.New(),
                CenarioId = model.CenarioId,
                Cliente = linha.Cliente,
                Material = linha.Material,
                Volume = linha.Volume,
                DataEntregaDesejada = linha.DataEntrega,
                TipoFreteEnum = linha.TipoFrete,
                SegmentoEnum = linha.Segmento
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
            Cliente = demanda.Cliente,
            Material = demanda.Material,
            Volume = demanda.Volume,
            DataEntregaDesejada = demanda.DataEntregaDesejada,
            TipoFrete = demanda.TipoFreteEnum.ToString(),
            Segmento = demanda.SegmentoEnum.ToString()
        };
    }
}