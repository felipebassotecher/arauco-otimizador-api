using Arauco.Otimizador.Data.Entities.Cenario;
using Arauco.Otimizador.Service.OtimizadorService;
using Arauco.Otimizador.Service.OtimizadorService.Capacidade;
using Arauco.Otimizador.Service.OtimizadorService.Dados;
using Arauco.Otimizador.Service.OtimizadorService.Modelo;
using Arauco.Otimizador.Service.OtimizadorV2Service.CriteriosV2;
using Google.OrTools.Sat;

namespace Arauco.Otimizador.Service.OtimizadorV2Service.Modelo;

public sealed record AlocacaoV2(
    int ItemIndice, int CentroId, int IndiceSemana, double VolumeM3, int ScorePeso);

public sealed record EmbarqueV2(
    string ClienteId, int CentroId, int IndiceSemana, int Carretas, double VolumeM3);

public sealed record ResultadoOtimizacaoV2(
    string Status,
    double Segundos,
    double Objetivo,
    IReadOnlyList<AlocacaoV2> Alocacoes,
    IReadOnlyDictionary<int, double> NaoAlocadoPorItem,
    int Variaveis,
    int Binarias,
    IReadOnlyList<EmbarqueV2> Embarques);

// Modelo CP-SAT do V2: mesma base de restrições de capacidade/lote mínimo/carreta do motor V1
// (Arauco.Otimizador.Service.OtimizadorService/Modelo/Otimizacao.cs), mas o objetivo é ponderado
// pelos critérios personalizados do cenário (CenarioCriterio) em vez de pesos fixos no código, e os
// itens já chegam com o volume pinado descontado (ver OtimizadorV2Service).
public static class OtimizacaoV2
{
    public const int Escala = 10;
    private const int BaseMultiplicador = 100;
    private const int MultiplicadorMinimo = 1;
    private const int MultiplicadorMaximo = 400;

    private static long Escalar(double m3) => (long)Math.Round(m3 * Escala);

    public static ResultadoOtimizacaoV2 Resolver(
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

        var x = new Dictionary<(int, int, int), IntVar>();
        var y = new Dictionary<(int, int, int), BoolVar>();
        var slack = new Dictionary<int, IntVar>();
        var contribuicao = new Dictionary<(int, int, int), LinearExpr>();
        var itensAtivos = 0;
        var indivisiveis = 0;

        foreach (var item in itens)
        {
            var volume = Escalar(item.VolumeM3);
            if (volume <= 0) continue; // volume já 100% coberto por pin — fora do modelo

            itensAtivos++;
            var piso = Escalar(item.LoteMinimoM3);
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

            if (piso >= volume)
            {
                indivisiveis++;
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
                continue;
            }

            var parcelas = new List<LinearExpr> { slack[i] };

            foreach (var (centro, s) in baldes)
            {
                var vx = modelo.NewIntVar(0, volume, $"x_{i}_{centro}_{s}");
                var vy = modelo.NewBoolVar($"y_{i}_{centro}_{s}");

                modelo.Add(vx <= volume * vy);
                modelo.Add(vx >= piso * vy);

                x[(i, centro, s)] = vx;
                y[(i, centro, s)] = vy;
                contribuicao[(i, centro, s)] = vx;
                parcelas.Add(vx);
            }

            modelo.Add(LinearExpr.Sum(parcelas) == volume);
        }

        notas.Add($"modelo: {indivisiveis} item(ns) indivisível(is) de {itensAtivos} "
                  + "(volume menor que um lote -> embarque todo-ou-nada, sem variável inteira)");

        // Carreta (m³ mín/máx por embarque) + limite de recebimento por cliente/semana — mesmo bloco
        // do V1, sem o termo de mix CIF/FOB (fora do escopo do V2).
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

        var alocacoes = new List<AlocacaoV2>();
        var naoAlocado = new Dictionary<int, double>();

        if (status is CpSolverStatus.Optimal or CpSolverStatus.Feasible)
        {
            foreach (var ((i, centro, s), _) in contribuicao)
            {
                long v = x.TryGetValue((i, centro, s), out var vx)
                    ? solver.Value(vx)
                    : (solver.Value(y[(i, centro, s)]) == 1 ? Escalar(itensPorIndice[i].VolumeM3) : 0);

                if (v > 0) alocacoes.Add(new AlocacaoV2(i, centro, s, v / (double)Escala, multiplicadores[i]));
            }

            foreach (var (i, var_) in slack)
            {
                var v = solver.Value(var_);
                if (v > 0) naoAlocado[i] = v / (double)Escala;
            }
        }

        var listaEmbarques = new List<EmbarqueV2>();

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
                    listaEmbarques.Add(new EmbarqueV2(
                        chave.Cliente, chave.Centro, chave.Semana, quantas, Math.Round(volume, 2)));
            }
        }

        return new ResultadoOtimizacaoV2(
            status.ToString(),
            solver.WallTime(),
            status is CpSolverStatus.Optimal or CpSolverStatus.Feasible ? solver.ObjectiveValue : double.NaN,
            alocacoes.OrderBy(a => a.IndiceSemana).ThenBy(a => a.CentroId).ToList(),
            naoAlocado,
            x.Count + slack.Count + carretas.Count,
            y.Count + embarques.Count,
            listaEmbarques.OrderByDescending(e => e.VolumeM3).ToList());
    }
}
