using Arauco.Otimizador.Common.Domain.Enums.Otimizador;
using Arauco.Otimizador.Data.Entities.Cenario;
using Arauco.Otimizador.Service.OtimizadorService.Capacidade;
using Arauco.Otimizador.Service.OtimizadorService.Dados;
using Google.OrTools.Sat;

namespace Arauco.Otimizador.Service.OtimizadorService.Modelo;

public sealed record MotivoAlocacao(CategoriaMotivoEnum Categoria, MotivoAlocacaoEnum Motivo);

public sealed record Alocacao(
    int ItemIndice, int CentroId, int IndiceSemana, double VolumeM3, int ScorePeso,
    IReadOnlyList<MotivoAlocacao> Motivos);

public sealed record Embarque(
    string ClienteId, int CentroId, int IndiceSemana, int Carretas, double VolumeM3);

public sealed record ResultadoOtimizacao(
    string Status,
    double Segundos,
    double Objetivo,
    IReadOnlyList<Alocacao> Alocacoes,
    IReadOnlyDictionary<int, double> NaoAlocadoPorItem,
    IReadOnlyDictionary<int, MotivoAlocacaoEnum> MotivoNaoAlocadoPorItem,
    int Variaveis,
    int Binarias,
    IReadOnlyList<Embarque> Embarques);

// Modelo CP-SAT do motor: restrições de carreta, com o objetivo ponderado pelos critérios
// personalizados do cenário (CenarioCriterio) em vez de pesos fixos no código. Os itens já chegam
// com o volume pinado descontado (ver OtimizadorService.DescontarPinados).
//
// Cada item (= cada demanda, ver Preparacao.cs) é alocado inteiro em no máximo um bucket (centro,
// semana) — nunca dividido entre vários. Isso garante que uma demanda gere, ao final, no máximo um
// pedido: ou o item inteiro cai em um único bucket, ou fica inteiro como "não alocado" (slack).
public static class Otimizacao
{
    public const int Escala = 10;
    private const int BaseMultiplicador = 100;
    private const int MultiplicadorMinimo = 1;
    private const int MultiplicadorMaximo = 400;

    private static long Escalar(double m3) => (long)Math.Round(m3 * Escala);

    public static ResultadoOtimizacao Resolver(
        Config config,
        IReadOnlyList<Item> itens,
        CapacidadeHorizonte capacidade,
        IReadOnlyList<CenarioCriterio> criterios,
        List<string> notas)
    {
        var semanas = capacidade.Semanas.Count;
        var itensPorIndice = itens.ToDictionary(i => i.Indice);

        var multiplicadores = itens.ToDictionary(
            i => i.Indice,
            i => Math.Clamp(BaseMultiplicador + AvaliadorCriterios.SomarPesos(i, criterios), MultiplicadorMinimo, MultiplicadorMaximo));

        var modelo = new CpModel();

        var y = new Dictionary<(int, int, int), BoolVar>();
        var slack = new Dictionary<int, IntVar>();
        var contribuicao = new Dictionary<(int, int, int), LinearExpr>();
        var itensAtivos = 0;

        foreach (var item in itens)
        {
            var volume = Escalar(item.VolumeM3);
            if (volume <= 0) continue; // volume já 100% coberto por pin — fora do modelo

            itensAtivos++;
            var i = item.Indice;

            var baldes = new List<(int Centro, int Semana)>();
            foreach (var centro in item.CentrosElegiveis)
                for (var s = 0; s < semanas; s++)
                    if (capacidade.Disponivel(centro, item.LinhaProdutoId, s) > 0)
                        baldes.Add((centro, s));

            if (baldes.Count == 0)
            {
                slack[i] = modelo.NewIntVar(volume, volume, $"slack_{i}");
                continue;
            }

            slack[i] = modelo.NewIntVar(0, volume, $"slack_{i}");

            var escolhas = new List<BoolVar>();

            foreach (var (centro, s) in baldes)
            {
                var vy = modelo.NewBoolVar($"y_{i}_{centro}_{s}");
                y[(i, centro, s)] = vy;
                escolhas.Add(vy);
                contribuicao[(i, centro, s)] = LinearExpr.Term(vy, volume);
            }

            modelo.Add(LinearExpr.Sum(escolhas) <= 1);
            modelo.Add(LinearExpr.Sum(escolhas) * volume + slack[i] == volume);
        }

        notas.Add($"modelo: {itensAtivos} item(ns) — cada um alocado inteiro em no máximo um "
                  + "bucket (centro, semana), sem divisão (1 demanda gera no máximo 1 pedido)");

        // Carreta (m³ mín/máx por embarque) + limite de recebimento por cliente/semana.
        var carretas = new Dictionary<(string Cliente, int Centro, int Semana), IntVar>();
        var embarques = new Dictionary<(string Cliente, int Centro, int Semana), BoolVar>();

        if (config.Carreta.Ativa)
        {
            var minCarreta = Escalar(config.Carreta.MinimoM3);
            var maxCarreta = Escalar(config.Carreta.MaximoM3);

            if (minCarreta <= 0 || maxCarreta < minCarreta)
                throw new InvalidOperationException(
                    $"Carreta inválida: mínimo {config.Carreta.MinimoM3} m3, máximo {config.Carreta.MaximoM3} m3.");

            var porEmbarque = new Dictionary<(string, int, int), List<LinearExpr>>();
            var volumeCliente = new Dictionary<string, double>();

            foreach (var ((i, centro, s), expr) in contribuicao)
            {
                var chave = (itensPorIndice[i].ClienteId, centro, s);
                if (!porEmbarque.TryGetValue(chave, out var lista))
                    porEmbarque[chave] = lista = [];
                lista.Add(expr);
            }

            foreach (var item in itens)
                volumeCliente[item.ClienteId] = volumeCliente.GetValueOrDefault(item.ClienteId) + item.VolumeM3;

            foreach (var (chave, parcelas) in porEmbarque)
            {
                var teto = Greedy.TetoCarretas(volumeCliente.GetValueOrDefault(chave.Item1), config);

                var n = modelo.NewIntVar(0, teto, $"n_{chave.Item1}_{chave.Item2}_{chave.Item3}");
                var b = modelo.NewBoolVar($"b_{chave.Item1}_{chave.Item2}_{chave.Item3}");

                var volumeEmbarque = LinearExpr.Sum(parcelas);

                modelo.Add(volumeEmbarque >= minCarreta * n);
                modelo.Add(volumeEmbarque <= maxCarreta * n);

                modelo.Add(n <= teto * b);
                modelo.Add(n >= b);

                carretas[chave] = n;
                embarques[chave] = b;
            }

            foreach (var ((i, centro, s), vy) in y)
            {
                var chave = (itensPorIndice[i].ClienteId, centro, s);
                if (embarques.TryGetValue(chave, out var b)) modelo.Add(vy <= b);
            }

            notas.Add($"carreta: {carretas.Count} embarque(s) (cliente, planta, semana), "
                      + $"{config.Carreta.MinimoM3:0.#}–{config.Carreta.MaximoM3:0.#} m3 por veículo");

            if (config.LimiteRecebimento.Ativo)
            {
                var limites = config.LimiteRecebimento;

                var porClienteSemana = new Dictionary<(string Cliente, int Semana), List<LinearExpr>>();
                foreach (var (chave, n) in carretas)
                {
                    var agrupador = (chave.Cliente, chave.Semana);
                    if (!porClienteSemana.TryGetValue(agrupador, out var lista))
                        porClienteSemana[agrupador] = lista = [];
                    lista.Add(n);
                }

                foreach (var (chave, ns) in porClienteSemana)
                    modelo.Add(LinearExpr.Sum(ns) <= limites.De(chave.Cliente));

                var clientesLimitados = porClienteSemana.Keys.Select(k => k.Cliente).Distinct().Count();
                notas.Add($"limite de recebimento: <= {limites.CarretasPorSemana} carreta(s) por "
                          + $"semana em {clientesLimitados} cliente(s), com {limites.PorCliente.Count} "
                          + "exceção(ões) por cliente");
            }
        }

        // Objetivo: minimizar o volume não atendido, ponderado pelo score dos critérios personalizados
        // que casam com cada item — item com mais peso acumulado custa mais caro deixar sem atender.
        var termoAtender = LinearExpr.Sum(slack.Select(kv =>
            LinearExpr.Term(kv.Value, multiplicadores[kv.Key])));

        modelo.Minimize(termoAtender);

        var solver = new CpSolver
        {
            StringParameters = $"max_time_in_seconds:{config.LimiteSegundos}"
                               + (config.LogSolver ? ",log_search_progress:true" : "")
                               + (config.Threads > 0 ? $",num_search_workers:{config.Threads}" : ""),
        };

        var status = solver.Solve(modelo);

        var alocacoes = new List<Alocacao>();
        var naoAlocado = new Dictionary<int, double>();
        var motivoNaoAlocado = new Dictionary<int, MotivoAlocacaoEnum>();

        if (status is CpSolverStatus.Optimal or CpSolverStatus.Feasible)
        {
            foreach (var ((i, centro, s), vy) in y)
            {
                if (solver.Value(vy) != 1) continue;

                var item = itensPorIndice[i];
                var v = Escalar(item.VolumeM3);

                var motivos = ComputarMotivosSemana(item, centro, s, capacidade)
                    .Select(m => new MotivoAlocacao(CategoriaMotivoEnum.PorqueNestaSemana, m))
                    .Append(new MotivoAlocacao(
                        CategoriaMotivoEnum.PorqueNesteCentro, ComputarMotivoCentro(item, capacidade, semanas)))
                    .ToList();

                alocacoes.Add(new Alocacao(i, centro, s, v / (double)Escala, multiplicadores[i], motivos));
            }

            foreach (var (i, var_) in slack)
            {
                var v = solver.Value(var_);
                if (v <= 0) continue;

                naoAlocado[i] = v / (double)Escala;

                var item = itensPorIndice[i];
                motivoNaoAlocado[i] = config.Carreta.Ativa && item.VolumeM3 < config.Carreta.MinimoM3
                    ? MotivoAlocacaoEnum.LoteMinimoMaiorQueParametrizado
                    : MotivoAlocacaoEnum.Despriorizado;
            }
        }

        var listaEmbarques = new List<Embarque>();

        if (config.Carreta.Ativa && status is CpSolverStatus.Optimal or CpSolverStatus.Feasible)
        {
            var volumePorEmbarque = alocacoes
                .GroupBy(a => (itensPorIndice[a.ItemIndice].ClienteId, a.CentroId, a.IndiceSemana))
                .ToDictionary(g => g.Key, g => g.Sum(a => a.VolumeM3));

            foreach (var (chave, n) in carretas)
            {
                var quantas = (int)solver.Value(n);
                var volume = volumePorEmbarque.GetValueOrDefault(chave, 0);
                if (quantas > 0 || volume > 0)
                    listaEmbarques.Add(new Embarque(
                        chave.Cliente, chave.Centro, chave.Semana, quantas, Math.Round(volume, 2)));
            }
        }

        return new ResultadoOtimizacao(
            status.ToString(),
            solver.WallTime(),
            status is CpSolverStatus.Optimal or CpSolverStatus.Feasible ? solver.ObjectiveValue : double.NaN,
            alocacoes.OrderBy(a => a.IndiceSemana).ThenBy(a => a.CentroId).ToList(),
            naoAlocado,
            motivoNaoAlocado,
            slack.Count + carretas.Count,
            y.Count + embarques.Count,
            listaEmbarques.OrderByDescending(e => e.VolumeM3).ToList());
    }

    // "Porque nesta semana": olha, no centro escolhido, todas as semanas anteriores à escolhida —
    // se em alguma delas não havia capacidade nenhuma, ou havia capacidade mas insuficiente para o
    // item inteiro, isso explica por que o item não pôde ser alocado antes. É heurístico (o CP-SAT
    // não expõe uma "razão" nativa para a escolha entre buckets empatados), não um traço literal do
    // solver.
    private static List<MotivoAlocacaoEnum> ComputarMotivosSemana(
        Item item, int centroEscolhido, int semanaEscolhidaIndice, CapacidadeHorizonte capacidade)
    {
        if (semanaEscolhidaIndice == 0)
            return [MotivoAlocacaoEnum.PrimeiraSemanaHorizonte];

        var motivos = new List<MotivoAlocacaoEnum>();

        for (var s = 0; s < semanaEscolhidaIndice; s++)
        {
            var disponivel = capacidade.Disponivel(centroEscolhido, item.LinhaProdutoId, s);

            if (disponivel <= 0)
            {
                if (!motivos.Contains(MotivoAlocacaoEnum.SemCapacidadeSemanasAnteriores))
                    motivos.Add(MotivoAlocacaoEnum.SemCapacidadeSemanasAnteriores);
            }
            else if (disponivel < item.VolumeM3 && !motivos.Contains(MotivoAlocacaoEnum.LoteMinimoNaoCabeAntes))
            {
                motivos.Add(MotivoAlocacaoEnum.LoteMinimoNaoCabeAntes);
            }
        }

        if (motivos.Count == 0)
            motivos.Add(MotivoAlocacaoEnum.SemCapacidadeSemanasAnteriores);

        return motivos;
    }

    // "Porque neste centro": conta quantos dos centros elegíveis para o item têm, em pelo menos uma
    // semana do horizonte, capacidade cadastrada para a linha de produto do item — se mais de um
    // centro atende, a escolha do solver entre eles foi uma decisão real (empate de custo); se só um
    // atende, não havia alternativa.
    private static MotivoAlocacaoEnum ComputarMotivoCentro(Item item, CapacidadeHorizonte capacidade, int semanas)
    {
        var centrosComCapacidade = item.CentrosElegiveis.Count(centro =>
            Enumerable.Range(0, semanas).Any(s => capacidade.Disponivel(centro, item.LinhaProdutoId, s) > 0));

        return centrosComCapacidade > 1
            ? MotivoAlocacaoEnum.PlantaEscolhidaEntreElegiveis
            : MotivoAlocacaoEnum.UnicaPlantaElegivelComCapacidade;
    }
}
