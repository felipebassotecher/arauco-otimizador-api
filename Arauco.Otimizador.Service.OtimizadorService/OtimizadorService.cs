using Arauco.Otimizador.Common.Domain.Enums.Cenario;
using Arauco.Otimizador.Common.Domain.Enums.Demanda;
using Arauco.Otimizador.Common.Domain.Enums.Otimizador;
using Arauco.Otimizador.Common.Domain.Models.Otimizador;
using Arauco.Otimizador.Common.Domain.Services.Otimizador;
using Arauco.Otimizador.Data.Entities;
using Arauco.Otimizador.Data.Entities.Otimizador;
using Arauco.Otimizador.Service.Base;
using Arauco.Otimizador.Service.OtimizadorService.Capacidade;
using Arauco.Otimizador.Service.OtimizadorService.Dados;
using Arauco.Otimizador.Service.OtimizadorService.Mapeamento;
using Arauco.Otimizador.Service.OtimizadorService.Modelo;
using Microsoft.EntityFrameworkCore;
using Techer.Common.Domain.Exceptions;
using Techer.Common.Domain.Interfaces;
using Techer.Common.Id;

namespace Arauco.Otimizador.Service.OtimizadorService;

public class OtimizadorService : ServiceBase, IOtimizadorService
{
    private readonly Executor _executor = new();

    public OtimizadorService(IUnitOfWork unitOfWork, IEnvironmentVariables environmentVariables)
        : base(unitOfWork, environmentVariables)
    {
    }

    public async Task<OtimizacaoResponse> OtimizarAsync(string cenarioId, OtimizacaoRequest? request)
    {
        var cenario = await unitOfWork.CenarioRepository
            .FirstOrDefaultAsync(c => c.CenarioId == cenarioId)
            ?? throw new NotFoundException("Cenário não encontrado");

        var demandas = await unitOfWork.DemandaRepository
            .Where(d => d.CenarioId == cenarioId)
            .ToListAsync();

        if (demandas.Count == 0)
            throw new ApiException("Cenário sem demandas carregadas");

        var criterios = await unitOfWork.CenarioCriterioRepository
            .Where(c => c.CenarioId == cenarioId)
            .ToListAsync();

        var pinados = await unitOfWork.PedidoOtimizadoRepository
            .Where(p => p.CenarioId == cenarioId && p.Pinado)
            .ToListAsync();

        var carregador = await _executor.CarregarAsync(unitOfWork);

        var carteira = DemandaParaCarteiraMapper.Mapear(demandas, carregador.Produtos);
        var dados = carregador.ComCarteira(carteira);

        var config = CriarConfig(request);
        var notas = new List<string>(dados.Notas);

        var bruta = ProvedorCapacidade.MontarBruta(config, dados.Capacidade, dados.ParesElegiveis, notas);
        var prep = Preparador.Preparar(dados, bruta.Pares, config, notas);

        var demandaPorLinha = prep.Itens
            .GroupBy(i => i.LinhaProdutoId)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.VolumeM3));
        var capacidade = ProvedorCapacidade.Aplicar(config, bruta, demandaPorLinha, notas);

        var (itensAjustados, capacidadeAjustada, volumePinadoTotal) = DescontarPinados(prep.Itens, capacidade, pinados, notas);

        var resultadoOtimizacao = Otimizacao.Resolver(config, itensAjustados, capacidadeAjustada, criterios, notas);

        var resultadoId = await IdGenerator.New();
        var geradoEm = DateTime.UtcNow;

        var alocadoNovo = resultadoOtimizacao.Alocacoes.Sum(a => a.VolumeM3);
        var naoAlocadoNovo = resultadoOtimizacao.NaoAlocadoPorItem.Values.Sum();

        var resultado = new CenarioOtimizacaoResultado
        {
            ResultadoId = resultadoId,
            CenarioId = cenarioId,
            GeradoEm = geradoEm,
            StatusSolver = resultadoOtimizacao.Status,
            Segundos = resultadoOtimizacao.Segundos,
            Objetivo = resultadoOtimizacao.Objetivo,
            Variaveis = resultadoOtimizacao.Variaveis,
            Binarias = resultadoOtimizacao.Binarias,
            CapacidadeTotal = capacidadeAjustada.Total,
            DemandaTotalM3 = (decimal)prep.DemandaTotalM3,
            DemandaElegivelM3 = (decimal)prep.DemandaElegivelM3,
            AlocadoM3 = (decimal)(alocadoNovo + volumePinadoTotal),
            NaoAlocadoM3 = (decimal)naoAlocadoNovo,
            Itens = prep.Itens.Count,
            ItensExcluidos = prep.Excluidos.Count
        };
        unitOfWork.CenarioOtimizacaoResultadoRepository.Add(resultado);

        // Pedidos fixados manualmente (pinado = true) permanecem intactos numa nova execução — mesmo
        // contrato que CenarioService.ProcessarAsync já usa para Pedido.Pinado.
        var pedidosGerados = await unitOfWork.PedidoOtimizadoRepository
            .Where(p => p.CenarioId == cenarioId && !p.Pinado)
            .ToListAsync();
        unitOfWork.PedidoOtimizadoRepository.RemoveRange(pedidosGerados);

        var itensPorIndice = itensAjustados.ToDictionary(i => i.Indice);
        var novosPedidos = new List<PedidoOtimizado>();
        var novosMotivos = new List<PedidoOtimizadoMotivo>();
        var motivosPorPedidoId = new Dictionary<string, List<PedidoOtimizadoMotivoResponse>>();

        foreach (var a in resultadoOtimizacao.Alocacoes)
        {
            var item = itensPorIndice[a.ItemIndice];
            var semana = capacidadeAjustada.Semanas[a.IndiceSemana];
            var centroNome = dados.Centros.FirstOrDefault(c => c.CentroId == a.CentroId)?.Nome ?? a.CentroId.ToString();
            var pedidoId = await IdGenerator.New(12);

            novosPedidos.Add(new PedidoOtimizado
            {
                PedidoId = pedidoId,
                CenarioId = cenarioId,
                ResultadoId = resultadoId,
                CarteiraId = item.CarteiraIds[0],
                Cliente = item.ClienteId,
                Material = item.ProdutoId,
                LinhaProdutoId = item.LinhaProdutoId,
                CentroId = a.CentroId,
                Centro = centroNome,
                TipoFreteEnum = item.Cif ? TipoFreteEnum.CIF : TipoFreteEnum.FOB,
                Industria = item.Industria,
                Volume = (decimal)a.VolumeM3,
                Ano = semana.Ano,
                Semana = semana.Numero,
                Pinado = false,
                ScorePeso = a.ScorePeso
            });

            novosMotivos.AddRange(a.Motivos.Select(m => new PedidoOtimizadoMotivo
            {
                PedidoId = pedidoId,
                CategoriaEnum = m.Categoria,
                MotivoEnum = m.Motivo
            }));

            motivosPorPedidoId[pedidoId] = a.Motivos
                .Select(m => new PedidoOtimizadoMotivoResponse { Categoria = m.Categoria, Motivo = m.Motivo })
                .ToList();
        }
        unitOfWork.PedidoOtimizadoRepository.AddRange(novosPedidos);
        unitOfWork.PedidoOtimizadoMotivoRepository.AddRange(novosMotivos);

        var naoAlocados = new List<PedidoOtimizadoNaoAlocado>();
        foreach (var (indice, volume) in resultadoOtimizacao.NaoAlocadoPorItem)
        {
            var item = itensPorIndice[indice];
            naoAlocados.Add(new PedidoOtimizadoNaoAlocado
            {
                NaoAlocadoId = await IdGenerator.New(12),
                ResultadoId = resultadoId,
                Cliente = item.ClienteId,
                Material = item.ProdutoId,
                LinhaProdutoId = item.LinhaProdutoId,
                VolumeM3 = (decimal)volume,
                Motivo = _DescreverMotivoNaoAlocado(resultadoOtimizacao.MotivoNaoAlocadoPorItem[indice]),
                CategoriaEnum = CategoriaMotivoEnum.PorqueNaoAlocado,
                MotivoEnum = resultadoOtimizacao.MotivoNaoAlocadoPorItem[indice]
            });
        }
        unitOfWork.PedidoOtimizadoNaoAlocadoRepository.AddRange(naoAlocados);

        await unitOfWork.SaveAsync();

        // Alinha com CenarioService.ProcessarAsync: qualquer um dos dois fluxos que gerar pedidos
        // marca o cenário como Processado, liberando o submeter (SubmeterAsync exige esse status).
        cenario.StatusEnum = StatusCenarioEnum.Processado;
        cenario.DataUltimoProcessamento = geradoEm;
        await unitOfWork.SaveAsync();

        return MapearResponse(resultado, resultadoOtimizacao, capacidadeAjustada, dados, itensPorIndice, notas, pinados, alocadoNovo, naoAlocadoNovo, novosPedidos, motivosPorPedidoId);
    }

    public async Task<List<PedidoOtimizadoResponse>> ListarPedidosDaSemanaAsync(string cenarioId, int ano, int semana)
    {
        if (!await unitOfWork.CenarioRepository.AnyAsync(c => c.CenarioId == cenarioId))
            throw new NotFoundException("Cenário não encontrado");

        var pedidos = await unitOfWork.PedidoOtimizadoRepository
            .Where(p => p.CenarioId == cenarioId && p.Ano == ano && p.Semana == semana)
            .ToListAsync();

        var pedidoIds = pedidos.Select(p => p.PedidoId).ToList();
        var motivos = await unitOfWork.PedidoOtimizadoMotivoRepository
            .Where(m => pedidoIds.Contains(m.PedidoId))
            .ToListAsync();
        var motivosPorPedido = motivos
            .GroupBy(m => m.PedidoId)
            .ToDictionary(g => g.Key, g => g.Select(_MapearMotivo).ToList());

        return pedidos.Select(p => MapearPedido(p, motivosPorPedido.GetValueOrDefault(p.PedidoId, []))).ToList();
    }

    // Itens que não couberam na capacidade disponível na última execução do motor de otimização
    // (ver PedidoOtimizadoNaoAlocado). Escopado ao ResultadoId mais recente do cenário — execuções
    // anteriores deixam seus próprios registros no banco, mas deixam de ser "atuais".
    public async Task<List<PedidoOtimizadoNaoAlocadoResponse>> ListarNaoAlocadosAsync(string cenarioId)
    {
        if (!await unitOfWork.CenarioRepository.AnyAsync(c => c.CenarioId == cenarioId))
            throw new NotFoundException("Cenário não encontrado");

        var ultimoResultado = await unitOfWork.CenarioOtimizacaoResultadoRepository
            .Where(r => r.CenarioId == cenarioId)
            .OrderByDescending(r => r.GeradoEm)
            .FirstOrDefaultAsync();

        if (ultimoResultado is null)
            return [];

        var naoAlocados = await unitOfWork.PedidoOtimizadoNaoAlocadoRepository
            .Where(n => n.ResultadoId == ultimoResultado.ResultadoId)
            .ToListAsync();

        return naoAlocados
            .Select(n => new PedidoOtimizadoNaoAlocadoResponse
            {
                Id = n.NaoAlocadoId,
                Cliente = n.Cliente,
                Material = n.Material,
                LinhaProdutoId = n.LinhaProdutoId,
                VolumeM3 = (double)n.VolumeM3,
                Motivo = n.Motivo,
                Motivos = n.CategoriaEnum.HasValue && n.MotivoEnum.HasValue
                    ? [new PedidoOtimizadoMotivoResponse { Categoria = n.CategoriaEnum.Value, Motivo = n.MotivoEnum.Value }]
                    : []
            })
            .OrderByDescending(n => n.VolumeM3)
            .ToList();
    }

    public async Task<PedidoOtimizadoResponse> MoverPedidoAsync(string cenarioId, MoverPedidoOtimizadoRequest model)
    {
        var pedido = await unitOfWork.PedidoOtimizadoRepository
            .FirstOrDefaultAsync(p => p.PedidoId == model.PedidoId && p.CenarioId == cenarioId)
            ?? throw new NotFoundException("Pedido não encontrado");

        pedido.Ano = model.AnoDestino;
        pedido.Semana = model.SemanaDestino;
        pedido.Pinado = true;

        await unitOfWork.SaveAsync();

        return MapearPedido(pedido, await _ObterMotivosAsync(pedido.PedidoId));
    }

    // Fixa ou libera um pedido na semana em que ele já está, sem movê-lo — ao contrário de
    // MoverPedidoAsync, que sempre fixa e sempre muda a semana. Um pedido pinado permanece intacto
    // (mesma semana/centro) em qualquer reotimização futura do cenário (ver DescontarPinados).
    public async Task<PedidoOtimizadoResponse> AlternarPinAsync(string cenarioId, AlternarPinPedidoRequest model)
    {
        var pedido = await unitOfWork.PedidoOtimizadoRepository
            .FirstOrDefaultAsync(p => p.PedidoId == model.PedidoId && p.CenarioId == cenarioId)
            ?? throw new NotFoundException("Pedido não encontrado");

        pedido.Pinado = !pedido.Pinado;

        await unitOfWork.SaveAsync();

        return MapearPedido(pedido, await _ObterMotivosAsync(pedido.PedidoId));
    }

    private async Task<List<PedidoOtimizadoMotivoResponse>> _ObterMotivosAsync(string pedidoId)
    {
        var motivos = await unitOfWork.PedidoOtimizadoMotivoRepository
            .Where(m => m.PedidoId == pedidoId)
            .ToListAsync();

        return motivos.Select(_MapearMotivo).ToList();
    }

    // Remove do lote a otimizar qualquer item cuja demanda (CarteiraId) já tenha um pedido pinado, e
    // desconta o volume desses pedidos da capacidade do bucket (centro, linha produto, semana)
    // correspondente — para que o solver nunca reotimize nem estoure capacidade já comprometida por
    // um pedido fixado manualmente numa execução anterior. Cada item corresponde a exatamente uma
    // demanda (ver Preparacao.cs), então "pinado" é sempre tudo-ou-nada por item, nunca parcial.
    private static (List<Item> Itens, CapacidadeHorizonte Capacidade, double VolumePinadoTotal) DescontarPinados(
        IReadOnlyList<Item> itens, CapacidadeHorizonte capacidade, List<PedidoOtimizado> pinados, List<string> notas)
    {
        if (pinados.Count == 0)
            return (itens.ToList(), capacidade, 0);

        var carteiraIdsPinados = pinados.Select(p => p.CarteiraId).ToHashSet();

        var itensAjustados = itens
            .Where(item => !item.CarteiraIds.Any(carteiraIdsPinados.Contains))
            .ToList();

        var indicePorSemana = capacidade.Semanas
            .Select((s, idx) => (s, idx))
            .ToDictionary(t => (t.s.Ano, t.s.Numero), t => t.idx);

        var consumoPorBucket = new Dictionary<(int Centro, int LinhaProduto, int IndiceSemana), long>();
        var foraDoHorizonte = 0;

        foreach (var p in pinados)
        {
            if (!indicePorSemana.TryGetValue((p.Ano, p.Semana), out var indiceSemana))
            {
                foraDoHorizonte++;
                continue;
            }

            var chave = (p.CentroId, p.LinhaProdutoId, indiceSemana);
            consumoPorBucket[chave] = consumoPorBucket.GetValueOrDefault(chave) + (long)Math.Round(p.Volume);
        }

        var novoPorBucket = new Dictionary<(int, int, int), long>(capacidade.PorBucket);
        foreach (var (chave, consumido) in consumoPorBucket)
            novoPorBucket[chave] = Math.Max(0, novoPorBucket.GetValueOrDefault(chave, 0) - consumido);

        var volumePinadoTotal = pinados.Sum(p => (double)p.Volume);

        notas.Add($"pinning: {pinados.Count} pedido(s) fixado(s) de execução(ões) anterior(es) "
                  + $"({volumePinadoTotal:N2} m3) removidos do lote a otimizar e descontados da "
                  + "capacidade"
                  + (foraDoHorizonte > 0 ? $" — {foraDoHorizonte} fixado(s) fora do horizonte atual, mantido(s) sem afetar capacidade" : ""));

        return (itensAjustados, capacidade with { PorBucket = novoPorBucket }, volumePinadoTotal);
    }

    private static Config CriarConfig(OtimizacaoRequest? request)
    {
        var config = new Config();

        if (request is null) return config;

        if (request.Horizonte.HasValue) config.Horizonte = request.Horizonte.Value;
        if (request.Capacidade.HasValue) config.Capacidade = (ModoCapacidade)request.Capacidade.Value;
        if (!string.IsNullOrWhiteSpace(request.SemanaInicial)) config.SemanaInicial = request.SemanaInicial;
        if (request.AlvoCapacidadeSobreDemanda.HasValue) config.AlvoCapacidadeSobreDemanda = request.AlvoCapacidadeSobreDemanda.Value;
        if (request.LimiteSegundos.HasValue) config.LimiteSegundos = request.LimiteSegundos.Value;
        if (request.CarretaMinimoM3.HasValue) config.Carreta.MinimoM3 = request.CarretaMinimoM3.Value;
        if (request.CarretaMaximoM3.HasValue) config.Carreta.MaximoM3 = request.CarretaMaximoM3.Value;
        if (request.LimiteRecebimentoCarretasPorSemana.HasValue)
        {
            config.LimiteRecebimento.Ativo = true;
            config.LimiteRecebimento.CarretasPorSemana = request.LimiteRecebimentoCarretasPorSemana.Value;
        }

        return config;
    }

    private static OtimizacaoResponse MapearResponse(
        CenarioOtimizacaoResultado resultado,
        ResultadoOtimizacao resultadoOtimizacao,
        CapacidadeHorizonte capacidade,
        Carregador dados,
        Dictionary<int, Item> itensPorIndice,
        List<string> notas,
        List<PedidoOtimizado> pinados,
        double alocadoNovo,
        double naoAlocadoNovo,
        List<PedidoOtimizado> novosPedidos,
        Dictionary<string, List<PedidoOtimizadoMotivoResponse>> motivosPorPedidoId)
    {
        var demandaElegivel = (double)resultado.DemandaElegivelM3;
        var alocadoTotal = alocadoNovo + pinados.Sum(p => (double)p.Volume);

        return new OtimizacaoResponse
        {
            ResultadoId = resultado.ResultadoId,
            GeradoEm = resultado.GeradoEm,
            Horizonte = capacidade.Semanas.Select(s => s.ToString()).ToList(),
            Solver = new OtimizacaoSolverResponse
            {
                Status = resultadoOtimizacao.Status,
                Segundos = Math.Round(resultadoOtimizacao.Segundos, 3),
                Objetivo = resultadoOtimizacao.Objetivo,
                Variaveis = resultadoOtimizacao.Variaveis,
                Binarias = resultadoOtimizacao.Binarias
            },
            Resumo = new OtimizacaoResumoResponse
            {
                DemandaTotalM3 = (double)resultado.DemandaTotalM3,
                DemandaElegivelM3 = demandaElegivel,
                AlocadoM3 = Math.Round(alocadoTotal, 2),
                NaoAlocadoM3 = Math.Round(naoAlocadoNovo, 2),
                CapacidadeTotal = resultado.CapacidadeTotal,
                PercentualAlocado = demandaElegivel > 0 ? Math.Round(alocadoTotal / demandaElegivel, 4) : 0,
                Itens = resultado.Itens,
                ItensExcluidos = resultado.ItensExcluidos
            },
            Alocacoes = novosPedidos.Select(p => new OtimizacaoAlocacaoResponse
            {
                Cliente = p.Cliente,
                Material = p.Material,
                LinhaProdutoId = p.LinhaProdutoId,
                CentroId = p.CentroId,
                Centro = p.Centro,
                TipoFrete = p.TipoFreteEnum.ToString(),
                TipoCliente = _TipoCliente(p.Industria),
                Volume = (double)p.Volume,
                Ano = p.Ano,
                Semana = p.Semana,
                Pinado = false,
                ScorePeso = p.ScorePeso,
                Motivos = motivosPorPedidoId.GetValueOrDefault(p.PedidoId, [])
            }).ToList(),
            NaoAlocado = resultadoOtimizacao.NaoAlocadoPorItem.Select(kv =>
            {
                var item = itensPorIndice[kv.Key];
                return new OtimizacaoNaoAlocadoResponse
                {
                    Cliente = item.ClienteId,
                    Material = item.ProdutoId,
                    LinhaProdutoId = item.LinhaProdutoId,
                    VolumeM3 = Math.Round(kv.Value, 2),
                    Motivo = _DescreverMotivoNaoAlocado(resultadoOtimizacao.MotivoNaoAlocadoPorItem[kv.Key])
                };
            }).OrderByDescending(n => n.VolumeM3).ToList(),
            Notas = notas
        };
    }

    private static PedidoOtimizadoResponse MapearPedido(PedidoOtimizado pedido, List<PedidoOtimizadoMotivoResponse> motivos)
    {
        return new PedidoOtimizadoResponse
        {
            Id = pedido.PedidoId,
            Cliente = pedido.Cliente,
            Material = pedido.Material,
            LinhaProdutoId = pedido.LinhaProdutoId,
            CentroId = pedido.CentroId,
            Centro = pedido.Centro,
            TipoFrete = pedido.TipoFreteEnum.ToString(),
            TipoCliente = _TipoCliente(pedido.Industria),
            Volume = pedido.Volume,
            Ano = pedido.Ano,
            Semana = pedido.Semana,
            Pinado = pedido.Pinado,
            ScorePeso = pedido.ScorePeso,
            Motivos = motivos
        };
    }

    // Mesma resolução usada pelo critério "Tipo de Cliente" (AvaliadorCriterios.ObterValorCampo).
    private static string _TipoCliente(bool industria) => industria ? "INDUSTRIA" : "REVENDA";

    private static PedidoOtimizadoMotivoResponse _MapearMotivo(PedidoOtimizadoMotivo motivo) => new()
    {
        Categoria = motivo.CategoriaEnum,
        Motivo = motivo.MotivoEnum
    };

    // Descrição livre persistida em PedidoOtimizadoNaoAlocado.Motivo (categoria "Porque não alocado" —
    // sem tela própria hoje, ao contrário dos pedidos alocados, que expõem Motivos estruturado via API).
    private static string _DescreverMotivoNaoAlocado(MotivoAlocacaoEnum motivo) => motivo switch
    {
        MotivoAlocacaoEnum.LoteMinimoMaiorQueParametrizado =>
            "lote mínimo maior que o parametrizado — volume do item abaixo da carreta mínima configurada",
        _ => "despriorizado — capacidade do horizonte foi alocada a itens de maior prioridade"
    };
}
