using Google.OrTools.Sat;
using Arauco.Otimizador.Service.OtimizadorService.Capacidade;
using Arauco.Otimizador.Service.OtimizadorService.Dados;

namespace Arauco.Otimizador.Service.OtimizadorService.Modelo;

public sealed record Alocacao(int ItemIndice, int CentroId, int IndiceSemana, double VolumeM3);

public sealed record Embarque(
    string ClienteId, int CentroId, int IndiceSemana, int Carretas, double VolumeM3);

public sealed record ResultadoOtimizacao(
    string Status,
    double Segundos,
    double Objetivo,
    IReadOnlyList<Alocacao> Alocacoes,
    IReadOnlyDictionary<int, double> NaoAlocadoPorItem,
    IReadOnlyDictionary<int, long> PrioridadePorItem,
    int Variaveis,
    int Binarias,
    IReadOnlyList<Embarque> Embarques,
    IReadOnlyList<Termo> Termos,
    double GreedyM3);

public static class Otimizacao
{
    public const int Escala = 10;
    public const int PrioridadeMaxima = 20;

    private static long Escalar(double m3) => (long)Math.Round(m3 * Escala);

    public static ResultadoOtimizacao Resolver(
        Config config,
        IReadOnlyList<Item> itens,
        CapacidadeHorizonte capacidade,
        List<string> notas)
    {
        var semanas = capacidade.Semanas.Count;
        var (prioridades, idades) = CalcularPrioridades(config.Pesos, itens);

        var modelo = new CpModel();

        var x = new Dictionary<(int, int, int), IntVar>();
        var y = new Dictionary<(int, int, int), BoolVar>();
        var slack = new IntVar[itens.Count];
        var contribuicao = new Dictionary<(int, int, int), LinearExpr>();
        var indivisiveis = 0;

        foreach (var item in itens)
        {
            var volume = Escalar(item.VolumeM3);
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

        notas.Add($"modelo: {indivisiveis} item(ns) indivisível(is) de {itens.Count} "
                  + "(volume menor que um lote -> embarque todo-ou-nada, sem variável inteira)");

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
                var chave = (itens[i].ClienteId, centro, s);
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
                var chave = (itens[i].ClienteId, centro, s);
                if (embarques.TryGetValue(chave, out var b)) modelo.Add(vy <= b);
            }

            notas.Add($"carreta: {carretas.Count} embarque(s) (cliente, planta, semana), "
                      + $"{config.Carreta.MinimoM3:0.#}–{config.Carreta.MaximoM3:0.#} m3 por veículo");

            if (config.LimiteRecebimento.Ativo)
            {
                var limites = config.LimiteRecebimento;

                if (limites.CarretasPorSemana < 0)
                    throw new InvalidOperationException(
                        $"LimiteRecebimento.CarretasPorSemana negativo: {limites.CarretasPorSemana}.");

                foreach (var (cliente, valor) in limites.PorCliente)
                    if (valor < 0)
                        throw new InvalidOperationException(
                            $"LimiteRecebimento.PorCliente[{cliente}] negativo: {valor}.");

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

                var clientes = porClienteSemana.Keys.Select(k => k.Cliente).Distinct().Count();
                notas.Add($"limite de recebimento: <= {limites.CarretasPorSemana} carreta(s) por "
                          + $"semana em {clientes} cliente(s), com {limites.PorCliente.Count} "
                          + "exceção(ões) por cliente");
            }
        }
        else
        {
            if (config.LimiteRecebimento.Ativo)
                notas.Add("ATENÇÃO limite de recebimento IGNORADO: depende da camada de "
                          + "carreta (Carreta.Ativa = false, não existe contagem de veículos)");
        }

        var porBucket = new Dictionary<(int, int, int), List<LinearExpr>>();
        var porCentroSemana = new Dictionary<(int, int), List<LinearExpr>>();

        foreach (var ((i, centro, s), expr) in contribuicao)
        {
            var linhaProduto = itens[i].LinhaProdutoId;

            if (!porBucket.TryGetValue((centro, linhaProduto, s), out var lista))
                porBucket[(centro, linhaProduto, s)] = lista = [];
            lista.Add(expr);

            if (!porCentroSemana.TryGetValue((centro, s), out var lista2))
                porCentroSemana[(centro, s)] = lista2 = [];
            lista2.Add(expr);
        }

        foreach (var ((centro, linhaProduto, s), vars) in porBucket)
            modelo.Add(LinearExpr.Sum(vars) <= Escalar(capacidade.Disponivel(centro, linhaProduto, s)));

        var centros = itens.SelectMany(i => i.CentrosElegiveis).Distinct().OrderBy(c => c).ToList();
        var picos = new Dictionary<int, IntVar>();

        foreach (var centro in centros)
        {
            var teto = Enumerable.Range(0, semanas)
                .Sum(s => porBucket
                    .Where(kv => kv.Key.Item1 == centro && kv.Key.Item3 == s)
                    .Sum(kv => Escalar(capacidade.Disponivel(centro, kv.Key.Item2, s))));

            var pico = modelo.NewIntVar(0, Math.Max(teto, 1), $"pico_{centro}");
            picos[centro] = pico;

            for (var s = 0; s < semanas; s++)
                if (porCentroSemana.TryGetValue((centro, s), out var vars) && vars.Count > 0)
                    modelo.Add(pico >= LinearExpr.Sum(vars));
        }

        var termos = new List<Termo>();

        var slackTotal = LinearExpr.Sum(itens.Select(i => (LinearExpr)slack[i.Indice]));
        termos.Add(new Termo
        {
            Nome = "atender",
            Descricao = "volume não atendido",
            Expressao = slackTotal,
            Ordem = config.Criterios.Atender,
        });

        termos.Add(new Termo
        {
            Nome = "antiguidade",
            Descricao = "deixar pedido antigo sem atender",
            Expressao = LinearExpr.Sum(itens.Select(i =>
                LinearExpr.Term(slack[i.Indice], (long)Math.Round(idades[i.Indice] * 10)))),
            Ordem = config.Criterios.Antiguidade,
        });

        termos.Add(new Termo
        {
            Nome = "industria",
            Descricao = "deixar cliente indústria sem atender",
            Expressao = LinearExpr.Sum(itens.Where(i => i.Industria)
                .Select(i => (LinearExpr)slack[i.Indice])),
            Ordem = config.Criterios.Industria,
        });

        var atraso = new List<LinearExpr>();
        foreach (var ((_, _, s), expr) in contribuicao)
            if (s > 0) atraso.Add(expr * s);

        termos.Add(new Termo
        {
            Nome = "atraso",
            Descricao = "volume empurrado para semanas tardias",
            Expressao = atraso.Count > 0 ? LinearExpr.Sum(atraso) : LinearExpr.Constant(0),
            Ordem = config.Criterios.Atraso,
        });

        termos.Add(new Termo
        {
            Nome = "suavizacao",
            Descricao = "pico de ocupação por planta",
            Expressao = picos.Count > 0
                ? LinearExpr.Sum(picos.Values.Select(p => (LinearExpr)p))
                : LinearExpr.Constant(0),
            Ordem = config.Criterios.Suavizacao,
        });

        var desvios = new List<LinearExpr>();
        var desviosPorBalde = new Dictionary<(int Centro, int Semana), (IntVar Baixo, IntVar Alto)>();

        if (config.MixFrete.Ativo)
        {
            var alvoMil = (long)Math.Round(config.MixFrete.AlvoCif * 1000);

            foreach (var (chave, todas) in porCentroSemana)
            {
                var cif = new List<LinearExpr>();
                foreach (var ((i, centro, s), expr) in contribuicao)
                    if (centro == chave.Item1 && s == chave.Item2 && itens[i].Cif)
                        cif.Add(expr);

                if (cif.Count == 0 && todas.Count == 0) continue;

                var total = LinearExpr.Sum(todas);
                var somaCif = cif.Count > 0 ? LinearExpr.Sum(cif) : LinearExpr.Constant(0);

                long teto = 0;
                foreach (var kv in capacidade.PorBucket)
                    if (kv.Key.Centro == chave.Item1 && kv.Key.IndiceSemana == chave.Item2)
                        teto += Escalar(kv.Value);
                teto = Math.Max(teto, 1);

                var folgaBaixo = modelo.NewIntVar(0, teto, $"cif_baixo_{chave.Item1}_{chave.Item2}");
                var folgaAlto = modelo.NewIntVar(0, teto, $"cif_alto_{chave.Item1}_{chave.Item2}");

                modelo.Add(folgaBaixo * 1000 >= total * alvoMil - somaCif * 1000);
                modelo.Add(folgaAlto * 1000 >= somaCif * 1000 - total * alvoMil);

                desvios.Add(folgaBaixo);
                desvios.Add(folgaAlto);
                desviosPorBalde[(chave.Item1, chave.Item2)] = (folgaBaixo, folgaAlto);
            }
        }

        termos.Add(new Termo
        {
            Nome = "mix_frete",
            Descricao = $"desvio do alvo de {config.MixFrete.AlvoCif:P0} CIF por planta x semana",
            Expressao = desvios.Count > 0 ? LinearExpr.Sum(desvios) : LinearExpr.Constant(0),
            Ordem = config.Criterios.MixFrete,
        });

        var ordensDuplicadas = termos.GroupBy(t => t.Ordem).Where(g => g.Count() > 1).ToList();
        if (ordensDuplicadas.Count > 0)
            notas.Add("ATENÇÃO: critérios com a MESMA ordem ("
                      + string.Join("; ", ordensDuplicadas.Select(g =>
                          $"{g.Key}: {string.Join(", ", g.Select(t => t.Nome))}"))
                      + ") — recebem peso igual e o desempate fica com o solver.");

        Objetivo.AplicarPesos(termos, config.Criterios.Modo);
        var diagnostico = Objetivo.Diagnosticar(termos, semanas, config.Criterios.Modo);
        if (diagnostico is not null) notas.Add(diagnostico);

        notas.Add("critérios (ordem: peso): " + string.Join(", ", termos
            .OrderBy(x => x.Ordem)
            .Select(x => $"{x.Ordem}.{x.Nome}:{x.Peso}")));

        var greedy = Greedy.Alocar(itens, prioridades, capacidade, semanas, config, Escalar);
        var hint = greedy.Alocacao;

        var problemas = Greedy.Validar(greedy, itens, capacidade, config, Escalar);
        if (problemas.Count > 0)
            notas.Add($"ATENÇÃO greedy infactível em {problemas.Count} ponto(s) — hint será "
                      + $"descartado pelo solver. Primeiros: {string.Join(" | ", problemas.Take(3))}");

        var hintVars = new List<IntVar>();
        var hintValores = new List<long>();
        double volumeHint = 0;

        foreach (var ((i, centro, s), vy) in y)
        {
            hintVars.Add(vy);
            hintValores.Add(hint.GetValueOrDefault((i, centro, s), 0L) > 0 ? 1 : 0);
        }

        foreach (var ((i, centro, s), vx) in x)
        {
            hintVars.Add(vx);
            hintValores.Add(hint.GetValueOrDefault((i, centro, s), 0L));
        }

        foreach (var item in itens)
        {
            var alocadoItem = hint.Where(kv => kv.Key.Item == item.Indice).Sum(kv => kv.Value);
            volumeHint += alocadoItem / (double)Escala;
            hintVars.Add(slack[item.Indice]);
            hintValores.Add(Math.Max(0, Escalar(item.VolumeM3) - alocadoItem));
        }

        foreach (var (centro, pico) in picos)
        {
            long maximo = 0;
            for (var s = 0; s < semanas; s++)
            {
                long naSemana = 0;
                foreach (var ((_, c, sem), v) in hint)
                    if (c == centro && sem == s) naSemana += v;
                maximo = Math.Max(maximo, naSemana);
            }
            hintVars.Add(pico);
            hintValores.Add(maximo);
        }

        foreach (var (chave, n) in carretas)
        {
            var quantas = greedy.Carretas.GetValueOrDefault(chave, 0);
            hintVars.Add(n);
            hintValores.Add(quantas);
            hintVars.Add(embarques[chave]);
            hintValores.Add(quantas > 0 ? 1 : 0);
        }

        if (desviosPorBalde.Count > 0)
        {
            var alvo = config.MixFrete.AlvoCif;

            foreach (var ((centro, s), (baixo, alto)) in desviosPorBalde)
            {
                long totalBalde = 0, cifBalde = 0;
                foreach (var ((i, c, sem), v) in hint)
                {
                    if (c != centro || sem != s) continue;
                    totalBalde += v;
                    if (itens[i].Cif) cifBalde += v;
                }

                var alvoVolume = alvo * totalBalde;
                hintVars.Add(baixo);
                hintValores.Add((long)Math.Max(0, Math.Ceiling(alvoVolume - cifBalde)));
                hintVars.Add(alto);
                hintValores.Add((long)Math.Max(0, Math.Ceiling(cifBalde - alvoVolume)));
            }
        }

        var variaveisDoModelo = modelo.Model.Variables.Count;

        if (hintVars.Count < variaveisDoModelo)
            notas.Add($"ATENÇÃO hint incompleto: {hintVars.Count} de {variaveisDoModelo} "
                      + "variáveis — o CP-SAT vai descartar o hint inteiro");

        for (var h = 0; h < hintVars.Count; h++)
            modelo.AddHint(hintVars[h], hintValores[h]);

        notas.Add($"greedy inicial: {volumeHint:N0} m3 alocados "
                  + $"({volumeHint / Math.Max(itens.Sum(i => i.VolumeM3), 1):P1} do elegível) — usado como hint");

        var solver = new CpSolver
        {
            StringParameters = $"max_time_in_seconds:{config.LimiteSegundos}"
                               + (config.LogSolver ? ",log_search_progress:true" : "")
                               + (config.Threads > 0 ? $",num_search_workers:{config.Threads}" : ""),
        };

        Dictionary<IntVar, long>? valoresLexico = null;
        var status = config.Criterios.Modo == ModoObjetivo.Lexicografico
            ? ResolverLexicografico(modelo, solver, config, termos, hintVars, notas, out valoresLexico)
            : ResolverPonderado(modelo, solver, termos);

        long ValorDe(IntVar v) => valoresLexico is not null ? valoresLexico[v] : solver.Value(v);

        var alocacoes = new List<Alocacao>();
        var naoAlocado = new Dictionary<int, double>();

        if (status is CpSolverStatus.Optimal or CpSolverStatus.Feasible)
        {
            foreach (var ((i, centro, s), _) in contribuicao)
            {
                long v = x.TryGetValue((i, centro, s), out var vx)
                    ? ValorDe(vx)
                    : (ValorDe(y[(i, centro, s)]) == 1 ? Escalar(itens[i].VolumeM3) : 0);

                if (v > 0) alocacoes.Add(new Alocacao(i, centro, s, v / (double)Escala));
            }

            foreach (var item in itens)
            {
                var v = ValorDe(slack[item.Indice]);
                if (v > 0) naoAlocado[item.Indice] = v / (double)Escala;
            }
        }

        var listaEmbarques = new List<Embarque>();

        if (config.Carreta.Ativa && status is CpSolverStatus.Optimal or CpSolverStatus.Feasible)
        {
            var volumePorEmbarque = alocacoes
                .GroupBy(a => (itens[a.ItemIndice].ClienteId, a.CentroId, a.IndiceSemana))
                .ToDictionary(g => g.Key, g => g.Sum(a => a.VolumeM3));

            foreach (var (chave, n) in carretas)
            {
                var quantas = (int)ValorDe(n);
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
            prioridades.Select((p, i) => (p, i)).ToDictionary(t => t.i, t => t.p),
            x.Count + slack.Length + picos.Count + carretas.Count,
            y.Count + embarques.Count,
            listaEmbarques.OrderByDescending(e => e.VolumeM3).ToList(),
            termos.OrderBy(x => x.Ordem).ToList(),
            volumeHint);
    }

    private static CpSolverStatus ResolverPonderado(
        CpModel modelo, CpSolver solver, IReadOnlyList<Termo> termos)
    {
        modelo.Minimize(Objetivo.Combinar(termos));
        var status = solver.Solve(modelo);

        if (status is CpSolverStatus.Optimal or CpSolverStatus.Feasible)
            foreach (var termo in termos)
                termo.Valor = solver.Value(termo.Expressao) / (double)Escala;

        return status;
    }

    private static CpSolverStatus ResolverLexicografico(
        CpModel modelo, CpSolver solver, Config config, IReadOnlyList<Termo> termos,
        IReadOnlyList<IntVar> todasVariaveis, List<string> notas,
        out Dictionary<IntVar, long>? valores)
    {
        var ordenados = termos.OrderBy(x => x.Ordem).ToList();
        var porNivel = Math.Max(config.LimiteSegundos / Math.Max(ordenados.Count, 1), 1);
        var tolerancia = config.Criterios.ToleranciaLexicografica;

        solver.StringParameters = $"max_time_in_seconds:{porNivel}"
                                  + (config.LogSolver ? ",log_search_progress:true" : "")
                                  + (config.Threads > 0 ? $",num_search_workers:{config.Threads}" : "");

        var status = CpSolverStatus.Unknown;
        var ultimoBom = CpSolverStatus.Unknown;
        valores = null;

        foreach (var termo in ordenados)
        {
            modelo.Minimize(termo.Expressao);
            status = solver.Solve(modelo);

            if (status is not (CpSolverStatus.Optimal or CpSolverStatus.Feasible))
            {
                notas.Add($"lexicográfico: nível '{termo.Nome}' terminou {status} — parando aqui "
                          + "e mantendo a melhor solução dos níveis anteriores");
                return valores is null ? status : ultimoBom;
            }

            ultimoBom = status;
            var valor = solver.Value(termo.Expressao);
            termo.Valor = valor / (double)Escala;

            var limite = (long)Math.Ceiling(valor * (1 + tolerancia));
            modelo.Add(termo.Expressao <= limite);

            valores = new Dictionary<IntVar, long>(todasVariaveis.Count);
            var hintProto = modelo.Model.SolutionHint;
            hintProto.Vars.Clear();
            hintProto.Values.Clear();
            foreach (var v in todasVariaveis)
            {
                var valorVar = solver.Value(v);
                valores[v] = valorVar;
                modelo.AddHint(v, valorVar);
            }

            notas.Add($"lexicográfico nível {termo.Ordem} '{termo.Nome}': "
                      + $"{termo.Valor:N0} (congelado em <= {limite / (double)Escala:N0}, "
                      + $"folga {tolerancia:P0}, {solver.WallTime():0.0}s)");
        }

        foreach (var termo in termos)
            termo.Valor = solver.Value(termo.Expressao) / (double)Escala;

        return status;
    }

    private static (long[] Prioridades, double[] Idades) CalcularPrioridades(
        Pesos pesos, IReadOnlyList<Item> itens)
    {
        var prioridades = new long[itens.Count];
        var idades = new double[itens.Count];
        if (itens.Count == 0) return (prioridades, idades);

        var ordemIdade = itens
            .OrderBy(i => i.DataDocumentoMaisAntiga)
            .Select((i, pos) => (i.Indice, pos))
            .ToDictionary(t => t.Indice, t => t.pos);
        var ultimo = Math.Max(itens.Count - 1, 1);

        foreach (var item in itens)
        {
            var idade = 1.0 - (ordemIdade[item.Indice] / (double)ultimo);
            idades[item.Indice] = idade;

            var score = pesos.Antiguidade * idade
                        + (item.Cif ? pesos.Cif : 0)
                        + (item.Industria ? pesos.Industria : 0);

            var somaPesos = Math.Max(pesos.Antiguidade + pesos.Cif + pesos.Industria, 1);
            var normalizado = score / somaPesos * PrioridadeMaxima;
            prioridades[item.Indice] = Math.Clamp((long)Math.Round(normalizado), 1, PrioridadeMaxima);
        }

        return (prioridades, idades);
    }
}
