using Arauco.Otimizador.Common.Domain.Enums.Cenario;
using Arauco.Otimizador.Common.Domain.Enums.Demanda;
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

        foreach (var a in resultadoOtimizacao.Alocacoes)
        {
            var item = itensPorIndice[a.ItemIndice];
            var semana = capacidadeAjustada.Semanas[a.IndiceSemana];
            var centroNome = dados.Centros.FirstOrDefault(c => c.CentroId == a.CentroId)?.Nome ?? a.CentroId.ToString();

            novosPedidos.Add(new PedidoOtimizado
            {
                PedidoId = await IdGenerator.New(12),
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
        }
        unitOfWork.PedidoOtimizadoRepository.AddRange(novosPedidos);

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
                Motivo = "sem capacidade suficiente no horizonte para o volume restante"
            });
        }
        unitOfWork.PedidoOtimizadoNaoAlocadoRepository.AddRange(naoAlocados);

        await unitOfWork.SaveAsync();

        // Alinha com CenarioService.ProcessarAsync: qualquer um dos dois fluxos que gerar pedidos
        // marca o cenário como Processado, liberando o submeter (SubmeterAsync exige esse status).
        cenario.StatusEnum = StatusCenarioEnum.Processado;
        cenario.DataUltimoProcessamento = geradoEm;
        await unitOfWork.SaveAsync();

        return MapearResponse(resultado, resultadoOtimizacao, capacidadeAjustada, dados, itensPorIndice, notas, pinados, alocadoNovo, naoAlocadoNovo, novosPedidos);
    }

    public async Task<List<PedidoOtimizadoResponse>> ListarPedidosDaSemanaAsync(string cenarioId, int ano, int semana)
    {
        if (!await unitOfWork.CenarioRepository.AnyAsync(c => c.CenarioId == cenarioId))
            throw new NotFoundException("Cenário não encontrado");

        var pedidos = await unitOfWork.PedidoOtimizadoRepository
            .Where(p => p.CenarioId == cenarioId && p.Ano == ano && p.Semana == semana)
            .ToListAsync();

        return pedidos.Select(MapearPedido).ToList();
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

        return MapearPedido(pedido);
    }

    // Desconta o volume já pinado do item (cliente+produto) e da capacidade do bucket (centro, linha
    // produto, semana) correspondente, para que o solver nunca re-otimize nem estoure capacidade já
    // comprometida por um pedido fixado manualmente numa execução anterior.
    private static (List<Item> Itens, CapacidadeHorizonte Capacidade, double VolumePinadoTotal) DescontarPinados(
        IReadOnlyList<Item> itens, CapacidadeHorizonte capacidade, List<PedidoOtimizado> pinados, List<string> notas)
    {
        if (pinados.Count == 0)
            return (itens.ToList(), capacidade, 0);

        var pinadoPorItem = pinados
            .GroupBy(p => (p.Cliente, p.Material))
            .ToDictionary(g => g.Key, g => g.Sum(p => (double)p.Volume));

        var itensAjustados = itens
            .Select(item =>
            {
                var pinadoVolume = pinadoPorItem.GetValueOrDefault((item.ClienteId, item.ProdutoId), 0);
                var restante = Math.Max(0, item.VolumeM3 - pinadoVolume);
                return item with { VolumeM3 = restante };
            })
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
                  + $"({volumePinadoTotal:N2} m3) descontados da demanda e da capacidade antes de "
                  + $"otimizar o restante"
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
        List<PedidoOtimizado> novosPedidos)
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
                ScorePeso = p.ScorePeso
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
                    Motivo = "sem capacidade suficiente no horizonte para o volume restante"
                };
            }).OrderByDescending(n => n.VolumeM3).ToList(),
            Notas = notas
        };
    }

    private static PedidoOtimizadoResponse MapearPedido(PedidoOtimizado pedido)
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
            ScorePeso = pedido.ScorePeso
        };
    }

    // Mesma resolução usada pelo critério "Tipo de Cliente" (AvaliadorCriterios.ObterValorCampo).
    private static string _TipoCliente(bool industria) => industria ? "INDUSTRIA" : "REVENDA";
}
