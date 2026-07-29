using Arauco.Otimizador.Common.Domain.Enums.Cenario;
using Arauco.Otimizador.Common.Domain.Models.Cenario;
using Arauco.Otimizador.Common.Domain.Models.Parametro;
using Arauco.Otimizador.Common.Domain.Models.Pedido;
using Arauco.Otimizador.Common.Domain.Services.Cenario;
using Arauco.Otimizador.Common.Domain.Util;
using Arauco.Otimizador.Common.Storage;
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

    public async Task<List<CenarioListaResponse>> ListarAsync()
    {
        var cenarios = await unitOfWork.CenarioRepository.AsQueryable().ToListAsync();

        var responses = new List<CenarioListaResponse>();

        foreach (var cenario in cenarios)
        {
            var (primeiraSemana, ultimaSemana) = await _ObterSemanasAsync(cenario.CenarioId);

            responses.Add(new CenarioListaResponse
            {
                Id = cenario.CenarioId,
                Nome = cenario.Nome,
                ArquivoNome = cenario.ArquivoNome,
                DataCriacao = cenario.DataCriacao,
                DataUltimoProcessamento = cenario.DataUltimoProcessamento,
                Status = cenario.StatusEnum,
                Submetido = cenario.Submetido,
                PrimeiraSemana = primeiraSemana,
                UltimaSemana = ultimaSemana
            });
        }

        return responses;
    }

    public async Task<CenarioDetalheResponse> ObterAsync(string cenarioId)
    {
        var cenario = await _ObterCenarioAsync(cenarioId);

        var parametros = await _ObterParametrosDoCenarioAsync(cenarioId);
        var (primeiraSemana, ultimaSemana) = await _ObterSemanasAsync(cenarioId);

        return new CenarioDetalheResponse
        {
            Id = cenario.CenarioId,
            Nome = cenario.Nome,
            Parametros = parametros,
            ArquivoNome = cenario.ArquivoNome,
            DataCriacao = cenario.DataCriacao,
            DataUltimoProcessamento = cenario.DataUltimoProcessamento,
            Status = cenario.StatusEnum,
            Submetido = cenario.Submetido,
            PrimeiraSemana = primeiraSemana,
            UltimaSemana = ultimaSemana
        };
    }

    public async Task<CenarioCriacaoResponse> CriarAsync(CenarioCriacaoRequest model)
    {
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

        if (model.ParametroIds != null)
        {
            foreach (var parametroId in model.ParametroIds)
            {
                unitOfWork.CenarioParametroRepository.Add(new CenarioParametro
                {
                    CenarioId = cenario.CenarioId,
                    ParametroId = parametroId
                });
            }
        }

        await unitOfWork.SaveAsync();

        var parametros = await _ObterParametrosDoCenarioAsync(cenario.CenarioId);

        return new CenarioCriacaoResponse
        {
            Id = cenario.CenarioId,
            Nome = cenario.Nome,
            Parametros = parametros,
            DataCriacao = cenario.DataCriacao,
            Status = cenario.StatusEnum
        };
    }

    public async Task<CenarioUploadArquivoResponse> UploadArquivoAsync(string cenarioId, string nomeArquivo, Stream conteudo)
    {
        var cenario = await _ObterCenarioAsync(cenarioId);

        using var buffer = new MemoryStream();
        await conteudo.CopyToAsync(buffer);

        if (buffer.Length == 0)
            throw new ApiException("Arquivo CSV vazio");

        buffer.Position = 0;
        await LocalFileStorageHelper.SaveAsync(environmentVariables, $"cenarios/{cenarioId}", nomeArquivo, buffer);

        buffer.Position = 0;
        using var reader = new StreamReader(buffer);
        var conteudoCsv = await reader.ReadToEndAsync();

        var linhas = DemandaCsvParser.Parse(conteudoCsv);

        var existentes = await unitOfWork.DemandaRepository.Where(d => d.CenarioId == cenarioId).ToListAsync();
        unitOfWork.DemandaRepository.RemoveRange(existentes);

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

        cenario.ArquivoNome = nomeArquivo;

        await unitOfWork.SaveAsync();

        var parametros = await _ObterParametrosDoCenarioAsync(cenarioId);
        var (primeiraSemana, ultimaSemana) = await _ObterSemanasAsync(cenarioId);

        return new CenarioUploadArquivoResponse
        {
            Id = cenario.CenarioId,
            Nome = cenario.Nome,
            Parametros = parametros,
            ArquivoNome = cenario.ArquivoNome,
            DataCriacao = cenario.DataCriacao,
            DataUltimoProcessamento = cenario.DataUltimoProcessamento,
            Status = cenario.StatusEnum,
            Submetido = cenario.Submetido,
            PrimeiraSemana = primeiraSemana,
            UltimaSemana = ultimaSemana
        };
    }

    public async Task RemoverAsync(string cenarioId)
    {
        var cenario = await _ObterCenarioAsync(cenarioId);

        var demandas = await unitOfWork.DemandaRepository.Where(d => d.CenarioId == cenarioId).ToListAsync();
        unitOfWork.DemandaRepository.RemoveRange(demandas);

        var pedidos = await unitOfWork.PedidoRepository.Where(p => p.CenarioId == cenarioId).ToListAsync();
        unitOfWork.PedidoRepository.RemoveRange(pedidos);

        var vinculos = await unitOfWork.CenarioParametroRepository.Where(c => c.CenarioId == cenarioId).ToListAsync();
        unitOfWork.CenarioParametroRepository.RemoveRange(vinculos);

        unitOfWork.CenarioRepository.Remove(cenario);

        await unitOfWork.SaveAsync();
    }

    public async Task<CenarioProcessamentoResponse> ProcessarAsync(string cenarioId)
    {
        var cenario = await _ObterCenarioAsync(cenarioId);

        if (cenario.Submetido)
            throw new SimultaneousAccessException();

        var demandas = await unitOfWork.DemandaRepository.Where(d => d.CenarioId == cenarioId).ToListAsync();

        var pedidosExistentes = await unitOfWork.PedidoRepository.Where(p => p.CenarioId == cenarioId).ToListAsync();
        var pedidosGerados = pedidosExistentes.Where(p => !p.Pinado).ToList();

        // Pedidos fixados manualmente (pinado = true) permanecem intactos numa nova execução.
        unitOfWork.PedidoRepository.RemoveRange(pedidosGerados);

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

        var parametros = await _ObterParametrosDoCenarioAsync(cenarioId);
        var (primeiraSemana, ultimaSemana) = await _ObterSemanasAsync(cenarioId);

        return new CenarioProcessamentoResponse
        {
            Id = cenario.CenarioId,
            Nome = cenario.Nome,
            Parametros = parametros,
            ArquivoNome = cenario.ArquivoNome,
            DataCriacao = cenario.DataCriacao,
            DataUltimoProcessamento = cenario.DataUltimoProcessamento,
            Status = cenario.StatusEnum,
            Submetido = cenario.Submetido,
            PrimeiraSemana = primeiraSemana,
            UltimaSemana = ultimaSemana
        };
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

    public async Task<List<PedidoListaResponse>> ListarPedidosDaSemanaAsync(string cenarioId, int ano, int semana)
    {
        await _ObterCenarioAsync(cenarioId);

        var pedidos = await unitOfWork
            .PedidoRepository
            .Where(p => p.CenarioId == cenarioId && p.Ano == ano && p.Semana == semana)
            .ToListAsync();

        return pedidos.Select(_MapPedidoLista).ToList();
    }

    public async Task<PedidoMovimentacaoResponse> MoverPedidoAsync(string cenarioId, PedidoMovimentacaoRequest model)
    {
        var pedido = await unitOfWork
            .PedidoRepository
            .FirstOrDefaultAsync(p => p.PedidoId == model.PedidoId && p.CenarioId == cenarioId) ?? throw new NotFoundException("Pedido não encontrado");

        pedido.Ano = model.AnoDestino;
        pedido.Semana = model.SemanaDestino;
        pedido.Pinado = true;

        await unitOfWork.SaveAsync();

        return new PedidoMovimentacaoResponse
        {
            Id = pedido.PedidoId,
            CenarioId = pedido.CenarioId,
            Cliente = pedido.Cliente,
            TipoFrete = pedido.TipoFreteEnum,
            Volume = pedido.Volume,
            DataEntregaPrevista = pedido.DataEntregaPrevista,
            Ano = pedido.Ano,
            Semana = pedido.Semana,
            Pinado = pedido.Pinado,
            Grupo = pedido.Grupo
        };
    }

    public async Task<CenarioSubmissaoResponse> SubmeterAsync(string cenarioId)
    {
        var cenario = await _ObterCenarioAsync(cenarioId);

        if (cenario.Submetido)
            throw new SimultaneousAccessException();

        if (cenario.StatusEnum != StatusCenarioEnum.Processado)
            throw new ApiException("Cenário precisa estar processado antes de ser submetido");

        cenario.Submetido = true;
        cenario.StatusEnum = StatusCenarioEnum.Submetido;

        await unitOfWork.SaveAsync();

        var parametros = await _ObterParametrosDoCenarioAsync(cenarioId);
        var (primeiraSemana, ultimaSemana) = await _ObterSemanasAsync(cenarioId);

        return new CenarioSubmissaoResponse
        {
            Id = cenario.CenarioId,
            Nome = cenario.Nome,
            Parametros = parametros,
            ArquivoNome = cenario.ArquivoNome,
            DataCriacao = cenario.DataCriacao,
            DataUltimoProcessamento = cenario.DataUltimoProcessamento,
            Status = cenario.StatusEnum,
            Submetido = cenario.Submetido,
            PrimeiraSemana = primeiraSemana,
            UltimaSemana = ultimaSemana
        };
    }

    private async Task<Cenario> _ObterCenarioAsync(string cenarioId)
    {
        return await unitOfWork
            .CenarioRepository
            .FirstOrDefaultAsync(c => c.CenarioId == cenarioId) ?? throw new NotFoundException("Cenário não encontrado");
    }

    private async Task<List<ParametroListaResponse>> _ObterParametrosDoCenarioAsync(string cenarioId)
    {
        var parametroIds = await unitOfWork
            .CenarioParametroRepository
            .Where(c => c.CenarioId == cenarioId)
            .Select(c => c.ParametroId)
            .ToListAsync();

        var parametros = await unitOfWork
            .ParametroRepository
            .Where(p => parametroIds.Contains(p.ParametroId))
            .ToListAsync();

        var valores = await unitOfWork
            .ParametroValorRepository
            .Where(v => parametroIds.Contains(v.ParametroId))
            .ToListAsync();

        return parametros
            .Select(p => new ParametroListaResponse
            {
                Id = p.ParametroId,
                Nome = p.Nome,
                Chave = p.Chave,
                Descricao = p.Descricao,
                Peso = p.Peso,
                Ativo = p.Ativo,
                Valores = _MapValores(valores.Where(v => v.ParametroId == p.ParametroId).ToList())
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

    private static List<ParametroValorResponse>? _MapValores(List<Data.Entities.Parametro.ParametroValor> valores)
    {
        if (valores.Count == 0)
            return null;

        return valores.Select(v => new ParametroValorResponse
        {
            Valor = v.Valor,
            Rotulo = v.Rotulo,
            Peso = v.Peso
        }).ToList();
    }

    private static PedidoListaResponse _MapPedidoLista(Pedido pedido)
    {
        return new PedidoListaResponse
        {
            Id = pedido.PedidoId,
            CenarioId = pedido.CenarioId,
            Cliente = pedido.Cliente,
            TipoFrete = pedido.TipoFreteEnum,
            Volume = pedido.Volume,
            DataEntregaPrevista = pedido.DataEntregaPrevista,
            Ano = pedido.Ano,
            Semana = pedido.Semana,
            Pinado = pedido.Pinado,
            Grupo = pedido.Grupo
        };
    }
}
