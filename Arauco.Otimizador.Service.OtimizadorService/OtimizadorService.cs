using Arauco.Otimizador.Common.Domain.Enums.Cenario;
using Arauco.Otimizador.Common.Domain.Enums.Demanda;
using Arauco.Otimizador.Common.Domain.Enums.Otimizador;
using Arauco.Otimizador.Common.Domain.Models.Otimizador;
using Arauco.Otimizador.Common.Domain.Services.Otimizador;
using Arauco.Otimizador.Data.Entities;
using Arauco.Otimizador.Data.Entities.Otimizador;
using Arauco.Otimizador.Data.Entities.Setup;
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

        if (cenario.SetupId is null)
            throw new ApiException("Cenário sem setup vinculado");

        var setup = await unitOfWork.SetupRepository
            .FirstOrDefaultAsync(s => s.SetupId == cenario.SetupId)
            ?? throw new ApiException("Setup vinculado ao cenário não encontrado");

        var ordemImportancia = await unitOfWork.SetupOrdemImportanciaRepository
            .Where(o => o.SetupId == setup.SetupId)
            .ToListAsync();

        var demandas = await unitOfWork.DemandaRepository
            .Where(d => d.CenarioId == cenarioId)
            .ToListAsync();

        if (demandas.Count == 0)
            throw new ApiException("Cenário sem demandas carregadas");

        var pinados = await unitOfWork.PedidoOtimizadoRepository
            .Where(p => p.CenarioId == cenarioId && p.Pinado)
            .ToListAsync();

        var carregador = await _executor.CarregarAsync(unitOfWork);

        var carteira = DemandaParaCarteiraMapper.Mapear(demandas, carregador.Produtos);
        var dados = carregador.ComCarteira(carteira);

        var config = CriarConfig(setup, request);
        var termos = Objetivo.CalcularPesos(ordemImportancia);
        var notas = new List<string>(dados.Notas);

        var bruta = ProvedorCapacidade.MontarBruta(config, dados.Capacidade, dados.ParesElegiveis, notas);
        var prep = Preparador.Preparar(dados, bruta.Pares, config, notas);

        var demandaPorLinha = prep.Itens
            .GroupBy(i => i.LinhaProdutoId)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.VolumeM3));
        var capacidade = ProvedorCapacidade.Aplicar(config, bruta, demandaPorLinha, notas);

        var (itensAjustados, capacidadeAjustada, volumePinadoTotal) = DescontarPinados(prep.Itens, capacidade, pinados, notas);

        var resultadoOtimizacao = Otimizacao.Resolver(config, itensAjustados, capacidadeAjustada, termos, notas);

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

    // Remove do lote a otimizar o volume já comprometido por pedidos pinados, e desconta o mesmo
    // volume da capacidade do bucket (centro, linha produto, semana) correspondente — para que o
    // solver nunca reotimize nem estoure capacidade já comprometida por um pedido fixado manualmente
    // numa execução anterior. Como um item agora agrega várias demandas do mesmo cliente+produto (ver
    // Preparacao.cs), o desconto passa a ser por grupo (Cliente, Produto), não mais por CarteiraId
    // exata: soma-se o volume pinado de cada grupo e subtrai-se do item correspondente, zerando/
    // excluindo o item quando o pinado já cobre tudo.
    private static (List<Item> Itens, CapacidadeHorizonte Capacidade, double VolumePinadoTotal) DescontarPinados(
        IReadOnlyList<Item> itens, CapacidadeHorizonte capacidade, List<PedidoOtimizado> pinados, List<string> notas)
    {
        if (pinados.Count == 0)
            return (itens.ToList(), capacidade, 0);

        var pinadoPorGrupo = pinados
            .GroupBy(p => (p.Cliente, p.Material))
            .ToDictionary(g => g.Key, g => g.Sum(p => (double)p.Volume));

        var itensAjustados = itens
            .Select(item =>
            {
                var pinado = pinadoPorGrupo.GetValueOrDefault((item.ClienteId, item.ProdutoId));
                if (pinado <= 0) return item;

                var restante = Math.Max(0, item.VolumeM3 - pinado);
                return item with { VolumeM3 = restante, Piso = Math.Min(item.Piso, restante) };
            })
            .Where(item => item.VolumeM3 > 0)
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
                  + $"({volumePinadoTotal:N2} m3) descontados do lote a otimizar (por cliente+produto) "
                  + "e da capacidade"
                  + (foraDoHorizonte > 0 ? $" — {foraDoHorizonte} fixado(s) fora do horizonte atual, mantido(s) sem afetar capacidade" : ""));

        return (itensAjustados, capacidade with { PorBucket = novoPorBucket }, volumePinadoTotal);
    }

    // Fonte de configuração do motor: o Setup vinculado ao cenário. Campos sem equivalente no Setup
    // continuam com os valores hard-coded de Config.cs (ver comentário na classe). `request` só
    // sobrepõe LimiteSegundos — um knob operacional por chamada, não uma regra de negócio do Setup.
    private static Config CriarConfig(Setup setup, OtimizacaoRequest? request)
    {
        var config = new Config();

        if (setup.Horizonte.HasValue) config.Horizonte = setup.Horizonte.Value;
        if (setup.ModoCapacidade.HasValue) config.Capacidade = (ModoCapacidade)setup.ModoCapacidade.Value;
        if (!string.IsNullOrWhiteSpace(setup.SemanaInicial)) config.SemanaInicial = setup.SemanaInicial;
        if (setup.AlvoCapacidadeSobreDemanda.HasValue) config.AlvoCapacidadeSobreDemanda = (double)setup.AlvoCapacidadeSobreDemanda.Value;
        if (setup.QuantidadeMinimaSkuPorLote.HasValue) config.QuantidadeMinimaSkuPorLote = setup.QuantidadeMinimaSkuPorLote.Value;

        if (setup.VolumeMinimoCarreta.HasValue && setup.VolumeMaximoCarreta.HasValue)
        {
            config.Carreta.Ativa = true;
            config.Carreta.MinimoM3 = (double)setup.VolumeMinimoCarreta.Value;
            config.Carreta.MaximoM3 = (double)setup.VolumeMaximoCarreta.Value;
        }

        if (setup.CapacidadeMaximaRecebimentoCliente.HasValue)
        {
            config.LimiteRecebimento.Ativo = true;
            config.LimiteRecebimento.CarretasPorSemana = setup.CapacidadeMaximaRecebimentoCliente.Value;
        }

        if (setup.MixTipoFrete.HasValue)
        {
            config.MixFrete.Ativo = true;
            config.MixFrete.AlvoCif = setup.MixTipoFrete.Value / 100.0;
        }

        if (request?.LimiteSegundos is { } limiteSegundos) config.LimiteSegundos = limiteSegundos;

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

    // Mesma resolução usada pelo campo "Tipo de Cliente" (Item.Industria — ver Preparacao.cs).
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
            "lote mínimo maior que o parametrizado — volume restante abaixo da carreta mínima configurada",
        _ => "despriorizado — capacidade do horizonte foi alocada a itens de maior prioridade"
    };
}
