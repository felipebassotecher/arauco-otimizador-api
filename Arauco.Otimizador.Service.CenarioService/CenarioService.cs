using Arauco.Otimizador.Common.Domain.Enums.Cenario;
using Arauco.Otimizador.Common.Domain.Enums.Criterio;
using Arauco.Otimizador.Common.Domain.Models.Cenario;
using Arauco.Otimizador.Common.Domain.Models.Criterio;
using Arauco.Otimizador.Common.Domain.Models.Pedido;
using Arauco.Otimizador.Common.Domain.Services.Cenario;
using Arauco.Otimizador.Common.Domain.Util;
using Arauco.Otimizador.Data.Entities;
using Arauco.Otimizador.Data.Entities.Cenario;
using Arauco.Otimizador.Data.Entities.Demanda;
using Arauco.Otimizador.Data.Entities.Pedido;
using Arauco.Otimizador.Service.Base;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using Techer.Common.Domain.Exceptions;
using Techer.Common.Domain.Interfaces;
using Techer.Common.Id;

namespace Arauco.Otimizador.Service.CenarioService;

public class CenarioService : ServiceBase, ICenarioService
{
    // Capacidade semanal de referência da planta, usada no cálculo de ocupação das métricas.
    // Não há configuração de capacidade real ainda; ajustar quando existir uma fonte oficial.
    private const decimal CapacidadeSemanalPlanta = 1000m;

    public CenarioService(IUnitOfWork unitOfWork, IEnvironmentVariables environmentVariables) : base(unitOfWork, environmentVariables)
    {
    }

    // Lista fixa (em código) dos critérios disponíveis — servida pela API (changelog 2026-08-03 /
    // spec §3.10.1). Não há CRUD/tabela para isso; ao adicionar um critério, ele entra em
    // CriteriosDisponiveis (Util/CriteriosDisponiveis.cs).
    public Task<List<CriterioDisponivelResponse>> ListarCriteriosDisponiveisAsync()
    {
        var response = CriteriosDisponiveis.Todos
            .Select(c => new CriterioDisponivelResponse
            {
                Chave = c.Chave,
                Nome = c.Nome,
                Tipo = c.Tipo
            })
            .ToList();

        return Task.FromResult(response);
    }

    public async Task<List<CenarioListaResponse>> ListarAsync()
    {
        var cenarios = await unitOfWork.CenarioRepository.AsQueryable().ToListAsync();

        return cenarios.Select(c => new CenarioListaResponse
        {
            Id = c.CenarioId,
            Nome = c.Nome,
            DataCriacao = c.DataCriacao,
            DataUltimoProcessamento = c.DataUltimoProcessamento,
            Submetido = c.Submetido
        }).ToList();
    }

    public async Task<CenarioDetalheResponse> ObterAsync(string cenarioId)
    {
        var cenario = await _ObterCenarioAsync(cenarioId);

        return await _MapDetalheAsync(cenario);
    }

    public async Task<CenarioCriacaoResponse> CriarAsync(CenarioCriacaoRequest model)
    {
        var criterios = _ValidarCriterios(model.Criterios);

        var cenario = new Cenario
        {
            CenarioId = await IdGenerator.New(),
            Nome = model.Nome,
            ArquivoNome = null,
            DataCriacao = DateTime.UtcNow,
            DataUltimoProcessamento = null,
            StatusEnum = StatusCenarioEnum.Pendente,
            Submetido = false
        };

        unitOfWork.CenarioRepository.Add(cenario);

        _PersistirCriterios(cenario.CenarioId, criterios);

        await unitOfWork.SaveAsync();

        return new CenarioCriacaoResponse { Id = cenario.CenarioId };
    }

    public async Task<CenarioDetalheResponse> AtualizarAsync(string cenarioId, CenarioAtualizacaoRequest model)
    {
        var cenario = await _ObterCenarioAsync(cenarioId);

        var criterios = _ValidarCriterios(model.Criterios);

        cenario.Nome = model.Nome;

        var criteriosExistentes = await unitOfWork
            .CenarioCriterioRepository
            .Where(c => c.CenarioId == cenarioId)
            .ToListAsync();
        unitOfWork.CenarioCriterioRepository.RemoveRange(criteriosExistentes);

        _PersistirCriterios(cenarioId, criterios);

        await unitOfWork.SaveAsync();

        return await _MapDetalheAsync(cenario);
    }

    public async Task<CenarioDetalheResponse> UploadArquivoAsync(string cenarioId, string nomeArquivo, Stream conteudo)
    {
        var cenario = await _ObterCenarioAsync(cenarioId);

        // Upload permitido apenas uma vez por cenário; o arquivo não pode ser substituído (spec §2.2).
        if (!string.IsNullOrEmpty(cenario.ArquivoNome))
            throw new SimultaneousAccessException();

        using var buffer = new MemoryStream();
        await conteudo.CopyToAsync(buffer);

        if (buffer.Length == 0)
            throw new ApiException("Arquivo CSV vazio");

        buffer.Position = 0;
        using var reader = new StreamReader(buffer);
        var conteudoCsv = await reader.ReadToEndAsync();

        var linhas = DemandaCsvParser.Parse(conteudoCsv);

        if (linhas.Count == 0)
            throw new ApiException("Arquivo CSV inválido ou vazio");

        var demandas = linhas.Select(linha => new Demanda
        {
            DemandaId = IdGenerator.NewSync(),
            CenarioId = cenarioId,
            Cliente = linha.Cliente,
            Material = linha.Material,
            Volume = linha.Volume,
            DataEntregaDesejada = linha.DataEntrega,
            TipoFreteEnum = linha.TipoFrete
        }).ToList();

        unitOfWork.DemandaRepository.AddRange(demandas);

        unitOfWork.CenarioArquivoRepository.Add(new CenarioArquivo
        {
            CenarioId = cenarioId,
            Nome = nomeArquivo,
            Conteudo = conteudoCsv,
            DataUpload = DateTime.UtcNow
        });

        cenario.ArquivoNome = nomeArquivo;

        await unitOfWork.SaveAsync();

        return await _MapDetalheAsync(cenario);
    }

    public async Task<(string Nome, string Conteudo)> DownloadArquivoAsync(string cenarioId)
    {
        await _ObterCenarioAsync(cenarioId);

        var arquivo = await unitOfWork
            .CenarioArquivoRepository
            .FirstOrDefaultAsync(a => a.CenarioId == cenarioId) ?? throw new NotFoundException("Nenhum arquivo de demandas carregado para este cenário");

        return (arquivo.Nome, arquivo.Conteudo);
    }

    public async Task RemoverAsync(string cenarioId)
    {
        var cenario = await _ObterCenarioAsync(cenarioId);

        var demandas = await unitOfWork.DemandaRepository.Where(d => d.CenarioId == cenarioId).ToListAsync();
        unitOfWork.DemandaRepository.RemoveRange(demandas);

        var pedidos = await unitOfWork.PedidoRepository.Where(p => p.CenarioId == cenarioId).ToListAsync();
        unitOfWork.PedidoRepository.RemoveRange(pedidos);

        var criterios = await unitOfWork.CenarioCriterioRepository.Where(c => c.CenarioId == cenarioId).ToListAsync();
        unitOfWork.CenarioCriterioRepository.RemoveRange(criterios);

        var arquivo = await unitOfWork.CenarioArquivoRepository.FirstOrDefaultAsync(a => a.CenarioId == cenarioId);
        if (arquivo != null)
            unitOfWork.CenarioArquivoRepository.Remove(arquivo);

        unitOfWork.CenarioRepository.Remove(cenario);

        await unitOfWork.SaveAsync();
    }

    public async Task<CenarioDetalheResponse> ProcessarAsync(string cenarioId)
    {
        var cenario = await _ObterCenarioAsync(cenarioId);

        if (cenario.Submetido)
            throw new SimultaneousAccessException();

        // Sem demandas (arquivoNome nulo) não há o que processar (spec §2.2).
        if (string.IsNullOrEmpty(cenario.ArquivoNome))
            throw new ApiException("Cenário sem demandas carregadas");

        var demandas = await unitOfWork.DemandaRepository.Where(d => d.CenarioId == cenarioId).ToListAsync();

        var pedidosExistentes = await unitOfWork.PedidoRepository.Where(p => p.CenarioId == cenarioId).ToListAsync();
        // Pedidos fixados manualmente (pinado = true) permanecem intactos numa nova execução (spec §5.4).
        var pedidosGerados = pedidosExistentes.Where(p => !p.Pinado).ToList();
        unitOfWork.PedidoRepository.RemoveRange(pedidosGerados);

        // Agrupamento por cliente + semana ISO (spec §5.3 — hoje agrupa por cliente; a aplicação dos
        // pesos dos critérios na otimização é futuramente, mantendo o contrato PedidoResponse).
        var novosPedidos = demandas
            .GroupBy(d => new
            {
                d.Cliente,
                Ano = ISOWeek.GetYear(d.DataEntregaDesejada),
                Semana = ISOWeek.GetWeekOfYear(d.DataEntregaDesejada)
            })
            .Select(grupo => new Pedido
            {
                PedidoId = IdGenerator.NewSync(),
                CenarioId = cenarioId,
                Cliente = grupo.Key.Cliente,
                TipoFreteEnum = grupo.First().TipoFreteEnum,
                Volume = grupo.Sum(d => d.Volume),
                DataEntregaPrevista = grupo.Max(d => d.DataEntregaDesejada),
                Ano = grupo.Key.Ano,
                Semana = grupo.Key.Semana,
                Pinado = false,
                Grupo = grupo.Key.Cliente
            })
            .ToList();

        unitOfWork.PedidoRepository.AddRange(novosPedidos);

        cenario.StatusEnum = StatusCenarioEnum.Processado;
        cenario.DataUltimoProcessamento = DateTime.UtcNow;

        await unitOfWork.SaveAsync();

        return await _MapDetalheAsync(cenario);
    }

    public async Task<CenarioMetricasResponse> ObterMetricasAsync(string cenarioId)
    {
        await _ObterCenarioAsync(cenarioId);

        var demandas = await unitOfWork.DemandaRepository.Where(d => d.CenarioId == cenarioId).ToListAsync();
        var pedidos = await unitOfWork.PedidoRepository.Where(p => p.CenarioId == cenarioId).ToListAsync();

        var volumePorSemana = pedidos
            .GroupBy(p => new { p.Ano, p.Semana })
            .OrderBy(g => g.Key.Ano).ThenBy(g => g.Key.Semana)
            .Select(g => new CenarioMetricaSemanaResponse
            {
                Ano = g.Key.Ano,
                Semana = g.Key.Semana,
                Volume = g.Sum(p => p.Volume),
                QuantidadePedidos = g.Count()
            })
            .ToList();

        var ocupacaoPlanta = volumePorSemana
            .Select(v => new CenarioOcupacaoPlantaResponse
            {
                Data = ISOWeek.ToDateTime(v.Ano, v.Semana, DayOfWeek.Monday),
                Percentual = (double)Math.Min(100m, Math.Round(v.Volume / CapacidadeSemanalPlanta * 100m, 2))
            })
            .ToList();

        return new CenarioMetricasResponse
        {
            QuantidadeDemandas = demandas.Count,
            QuantidadePedidos = pedidos.Count,
            VolumeTotal = demandas.Sum(d => d.Volume),
            VolumePorSemana = volumePorSemana,
            OcupacaoPlanta = ocupacaoPlanta
        };
    }

    public async Task<List<PedidoResponse>> ListarPedidosDaSemanaAsync(string cenarioId, int ano, int semana)
    {
        await _ObterCenarioAsync(cenarioId);

        var pedidos = await unitOfWork
            .PedidoRepository
            .Where(p => p.CenarioId == cenarioId && p.Ano == ano && p.Semana == semana)
            .ToListAsync();

        return pedidos.Select(_MapPedido).ToList();
    }

    public async Task<PedidoResponse> MoverPedidoAsync(string cenarioId, MoverPedidoRequest model)
    {
        var pedido = await unitOfWork
            .PedidoRepository
            .FirstOrDefaultAsync(p => p.PedidoId == model.PedidoId && p.CenarioId == cenarioId) ?? throw new NotFoundException("Pedido não encontrado");

        pedido.Ano = model.AnoDestino;
        pedido.Semana = model.SemanaDestino;
        pedido.Pinado = true;

        await unitOfWork.SaveAsync();

        return _MapPedido(pedido);
    }

    public async Task<CenarioDetalheResponse> SubmeterAsync(string cenarioId)
    {
        var cenario = await _ObterCenarioAsync(cenarioId);

        if (cenario.Submetido)
            throw new SimultaneousAccessException();

        if (cenario.StatusEnum != StatusCenarioEnum.Processado)
            throw new ApiException("Cenário precisa estar processado antes de ser submetido");

        cenario.Submetido = true;
        cenario.StatusEnum = StatusCenarioEnum.Submetido;

        await unitOfWork.SaveAsync();

        return await _MapDetalheAsync(cenario);
    }

    private async Task<Cenario> _ObterCenarioAsync(string cenarioId)
    {
        return await unitOfWork
            .CenarioRepository
            .FirstOrDefaultAsync(c => c.CenarioId == cenarioId) ?? throw new NotFoundException("Cenário não encontrado");
    }

    private async Task<CenarioDetalheResponse> _MapDetalheAsync(Cenario cenario)
    {
        var criterios = await _ObterCriteriosDoCenarioAsync(cenario.CenarioId);
        var (primeiraSemana, ultimaSemana) = await _ObterSemanasAsync(cenario.CenarioId);

        return new CenarioDetalheResponse
        {
            Id = cenario.CenarioId,
            Nome = cenario.Nome,
            Criterios = criterios,
            ArquivoNome = cenario.ArquivoNome,
            DataCriacao = cenario.DataCriacao,
            DataUltimoProcessamento = cenario.DataUltimoProcessamento,
            Status = cenario.StatusEnum,
            Submetido = cenario.Submetido,
            PrimeiraSemana = primeiraSemana,
            UltimaSemana = ultimaSemana
        };
    }

    private async Task<List<CriterioRegraResponse>> _ObterCriteriosDoCenarioAsync(string cenarioId)
    {
        var criterios = await unitOfWork
            .CenarioCriterioRepository
            .Where(c => c.CenarioId == cenarioId)
            .ToListAsync();

        return criterios
            .Select(c => new CriterioRegraResponse
            {
                CriterioChave = CriteriosDisponiveis.ObterChaveEnum(c.CriterioChave) ?? CriterioChaveEnum.TipoFrete,
                Operador = c.Operador,
                Valor = c.Valor,
                Peso = c.Peso
            })
            .ToList();
    }

    private async Task<(SemanaAnoResponse? PrimeiraSemana, SemanaAnoResponse? UltimaSemana)> _ObterSemanasAsync(string cenarioId)
    {
        var pedidos = await unitOfWork
            .PedidoRepository
            .Where(p => p.CenarioId == cenarioId)
            .ToListAsync();

        if (pedidos.Count == 0)
            return (null, null);

        var ordenados = pedidos.OrderBy(p => p.Ano).ThenBy(p => p.Semana).ToList();

        var primeiraSemana = new SemanaAnoResponse { Ano = ordenados.First().Ano, Semana = ordenados.First().Semana };
        var ultimaSemana = new SemanaAnoResponse { Ano = ordenados.Last().Ano, Semana = ordenados.Last().Semana };

        return (primeiraSemana, ultimaSemana);
    }

    // Valida a lista de regras de critério (spec §3.9/§5.10): chave existe, operador compatível com o
    // tipo do critério, peso entre -100 e 100. Retorna a lista validada. Lança ApiException (400).
    private static List<CriterioRegraRequest> _ValidarCriterios(List<CriterioRegraRequest> criterios)
    {
        if (criterios == null || criterios.Count == 0)
            throw new ApiException("Cenário deve ter ao menos um critério");

        foreach (var regra in criterios)
        {
            var tipo = CriteriosDisponiveis.ObterTipo(regra.CriterioChave)
                ?? throw new ApiException($"Critério '{regra.CriterioChave}' não é um critério disponível");

            if (!CriteriosDisponiveis.OperadorCompativel(tipo, regra.Operador))
                throw new ApiException($"Operador '{regra.Operador}' não é compatível com o critério '{regra.CriterioChave}' ({tipo})");

            if (regra.Peso < -100 || regra.Peso > 100)
                throw new ApiException("Peso deve estar entre -100 e 100");
        }

        return criterios;
    }

    private void _PersistirCriterios(string cenarioId, List<CriterioRegraRequest> criterios)
    {
        foreach (var regra in criterios)
        {
            unitOfWork.CenarioCriterioRepository.Add(new CenarioCriterio
            {
                CenarioId = cenarioId,
                CriterioChave = CriteriosDisponiveis.ObterChaveString(regra.CriterioChave),
                Operador = regra.Operador,
                Valor = regra.Valor,
                Peso = regra.Peso
            });
        }
    }

    private static PedidoResponse _MapPedido(Pedido pedido)
    {
        return new PedidoResponse
        {
            Id = pedido.PedidoId,
            Cliente = pedido.Cliente,
            TipoFrete = pedido.TipoFreteEnum.ToString(),
            Volume = pedido.Volume,
            DataEntregaPrevista = pedido.DataEntregaPrevista,
            Ano = pedido.Ano,
            Semana = pedido.Semana,
            Pinado = pedido.Pinado
        };
    }
}