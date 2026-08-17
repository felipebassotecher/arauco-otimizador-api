using Arauco.Otimizador.Common.Domain.Enums.Cenario;
using Arauco.Otimizador.Common.Domain.Models.Cenario;
using Arauco.Otimizador.Common.Domain.Models.Pedido;
using Arauco.Otimizador.Common.Domain.Services.Cenario;
using Arauco.Otimizador.Common.Domain.Util;
using Arauco.Otimizador.Data.Entities;
using Arauco.Otimizador.Data.Entities.Cenario;
using Arauco.Otimizador.Data.Entities.Demanda;
using Arauco.Otimizador.Data.Entities.Pedido;
using Arauco.Otimizador.Service.Base;
using Arauco.Otimizador.Service.OtimizadorService.Dados;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using Techer.Common.Domain.Exceptions;
using Techer.Common.Domain.Interfaces;
using Techer.Common.Id;

namespace Arauco.Otimizador.Service.CenarioService;

public class CenarioService : ServiceBase, ICenarioService
{
    public CenarioService(IUnitOfWork unitOfWork, IEnvironmentVariables environmentVariables) : base(unitOfWork, environmentVariables)
    {
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
        if (string.IsNullOrWhiteSpace(model.SetupId))
            throw new ApiException("Setup é obrigatório");

        if (!await unitOfWork.SetupRepository.AnyAsync(s => s.SetupId == model.SetupId))
            throw new ApiException("Setup não encontrado");

        var cenario = new Cenario
        {
            CenarioId = await IdGenerator.New(),
            Nome = model.Nome,
            SetupId = model.SetupId,
            ArquivoNome = null,
            DataCriacao = DateTime.UtcNow,
            DataUltimoProcessamento = null,
            StatusEnum = StatusCenarioEnum.Pendente,
            Submetido = false
        };

        unitOfWork.CenarioRepository.Add(cenario);

        await unitOfWork.SaveAsync();

        return new CenarioCriacaoResponse { Id = cenario.CenarioId };
    }

    // O setup vinculado é fixado na criação (CenarioCriacaoRequest.SetupId) e não é editável aqui —
    // ver comentário em CenarioAtualizacaoRequest.
    public async Task<CenarioDetalheResponse> AtualizarAsync(string cenarioId, CenarioAtualizacaoRequest model)
    {
        var cenario = await _ObterCenarioAsync(cenarioId);

        cenario.Nome = model.Nome;

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
            DemandaId = IdGenerator.NewSync(12),
            CenarioId = cenarioId,
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

        var arquivo = await unitOfWork.CenarioArquivoRepository.FirstOrDefaultAsync(a => a.CenarioId == cenarioId);
        if (arquivo != null)
            unitOfWork.CenarioArquivoRepository.Remove(arquivo);

        unitOfWork.CenarioRepository.Remove(cenario);

        await unitOfWork.SaveAsync();
    }

    public async Task<ProcessarCenarioResponse> ProcessarAsync(string cenarioId)
    {
        var cenario = await _ObterCenarioAsync(cenarioId);

        if (cenario.Submetido)
            throw new SimultaneousAccessException();

        // Sem demandas (arquivoNome nulo) não há o que processar (spec §2.2).
        if (string.IsNullOrEmpty(cenario.ArquivoNome))
            throw new ApiException("Cenário sem demandas carregadas");

        var inicio = DateTime.UtcNow;

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
                PedidoId = IdGenerator.NewSync(12),
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

        var tempoSegundos = (DateTime.UtcNow - inicio).TotalSeconds;

        return new ProcessarCenarioResponse
        {
            Sucesso = true,
            TempoSegundos = tempoSegundos
        };
    }

    public async Task<CenarioMetricasResponse> ObterMetricasAsync(string cenarioId)
    {
        await _ObterCenarioAsync(cenarioId);

        var demandas = await unitOfWork.DemandaRepository.Where(d => d.CenarioId == cenarioId).ToListAsync();

        // Pedidos e ocupação vêm do motor de otimização (PedidoOtimizado), não do fluxo simples
        // (Pedido) — mesmo dado que a aba "Pedidos" (OtimizacaoService) e o botão "Processar"
        // (POST /otimizar) usam, para que Métricas e Pedidos sempre concordem entre si.
        var pedidos = await unitOfWork.PedidoOtimizadoRepository.Where(p => p.CenarioId == cenarioId).ToListAsync();

        var ultimoResultado = await unitOfWork
            .CenarioOtimizacaoResultadoRepository
            .Where(r => r.CenarioId == cenarioId)
            .OrderByDescending(r => r.GeradoEm)
            .FirstOrDefaultAsync();

        var naoAlocados = ultimoResultado != null
            ? await unitOfWork.PedidoOtimizadoNaoAlocadoRepository.Where(n => n.ResultadoId == ultimoResultado.ResultadoId).ToListAsync()
            : [];

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

        // Capacidade real declarada por planta/semana (mesma leitura do master data que o motor de
        // otimização usa — Carregador —, mas sem a calibração/simulação específica de cada execução;
        // ver comentário em CenarioOcupacaoPlantaResponse).
        var carregador = await Carregador.CarregarAsync(unitOfWork);

        var capacidadePorCentroSemana = carregador.Capacidade
            .GroupBy(c => (c.CentroId, c.Semana))
            .ToDictionary(g => g.Key, g => g.Sum(c => c.Quantidade));

        // Horizonte do cenário = janela contínua da primeira à última semana com pedido alocado.
        // Não fica persistido o horizonte configurado na execução (Config.Horizonte), então esta é a
        // melhor aproximação disponível a partir do que de fato foi otimizado.
        var semanasHorizonte = new List<SemanaIso>();
        if (pedidos.Count > 0)
        {
            var semanasPedidos = pedidos.Select(p => new SemanaIso(p.Ano, p.Semana)).ToList();
            var semanaMin = semanasPedidos.Min();
            var semanaMax = semanasPedidos.Max();

            for (var semana = semanaMin; semana.CompareTo(semanaMax) <= 0; semana = semana.Somar(1))
                semanasHorizonte.Add(semana);
        }

        var volumeAlocadoPorCentro = pedidos
            .GroupBy(p => p.CentroId)
            .ToDictionary(g => g.Key, g => g.Sum(p => p.Volume));

        var ocupacaoPlanta = carregador.Centros
            .Select(centro =>
            {
                var capacidadeTotal = semanasHorizonte
                    .Sum(s => capacidadePorCentroSemana.GetValueOrDefault((centro.CentroId, s), 0L));
                var volumeAlocado = volumeAlocadoPorCentro.GetValueOrDefault(centro.CentroId, 0m);

                var percentual = capacidadeTotal > 0
                    ? (double)Math.Min(100m, Math.Round(volumeAlocado / capacidadeTotal * 100m, 2))
                    : (volumeAlocado > 0 ? 100d : 0d);

                return new CenarioOcupacaoPlantaResponse
                {
                    CentroId = centro.CentroId,
                    Centro = centro.Nome,
                    CapacidadeTotalM3 = capacidadeTotal,
                    VolumeAlocadoM3 = (double)volumeAlocado,
                    Percentual = percentual
                };
            })
            .ToList();

        return new CenarioMetricasResponse
        {
            QuantidadeDemandas = demandas.Count,
            QuantidadePedidos = pedidos.Count,
            PedidosAlocados = pedidos.Count,
            PedidosNaoAlocados = naoAlocados.Count,
            VolumeTotal = demandas.Sum(d => d.Volume),
            VolumeTotalAlocado = ultimoResultado?.AlocadoM3 ?? pedidos.Sum(p => p.Volume),
            VolumeTotalNaoAlocado = ultimoResultado?.NaoAlocadoM3 ?? naoAlocados.Sum(n => n.VolumeM3),
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
        var setup = cenario.SetupId != null
            ? await unitOfWork.SetupRepository.FirstOrDefaultAsync(s => s.SetupId == cenario.SetupId)
            : null;
        var (primeiraSemana, ultimaSemana) = await _ObterSemanasAsync(cenario.CenarioId);

        return new CenarioDetalheResponse
        {
            Id = cenario.CenarioId,
            Nome = cenario.Nome,
            SetupId = cenario.SetupId,
            SetupNome = setup?.Nome,
            ArquivoNome = cenario.ArquivoNome,
            DataCriacao = cenario.DataCriacao,
            DataUltimoProcessamento = cenario.DataUltimoProcessamento,
            Status = cenario.StatusEnum,
            Submetido = cenario.Submetido,
            PrimeiraSemana = primeiraSemana,
            UltimaSemana = ultimaSemana
        };
    }

    // Baseado em PedidoOtimizado (motor CP-SAT via POST /otimizar), não no fluxo simples (Pedido/
    // POST /processar): a aba "Pedidos" do front só lê PedidoOtimizado (OtimizacaoService), então
    // primeiraSemana/ultimaSemana precisam apontar para semanas que essa aba de fato tem dados.
    private async Task<(SemanaAnoResponse? PrimeiraSemana, SemanaAnoResponse? UltimaSemana)> _ObterSemanasAsync(string cenarioId)
    {
        var pedidos = await unitOfWork
            .PedidoOtimizadoRepository
            .Where(p => p.CenarioId == cenarioId)
            .ToListAsync();

        if (pedidos.Count == 0)
            return (null, null);

        var ordenados = pedidos.OrderBy(p => p.Ano).ThenBy(p => p.Semana).ToList();

        var primeiraSemana = new SemanaAnoResponse { Ano = ordenados.First().Ano, Semana = ordenados.First().Semana };
        var ultimaSemana = new SemanaAnoResponse { Ano = ordenados.Last().Ano, Semana = ordenados.Last().Semana };

        return (primeiraSemana, ultimaSemana);
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