using Arauco.Otimizador.Common.Domain.Enums.Otimizador;
using Arauco.Otimizador.Common.Domain.Enums.Setup;
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

// Modelo CP-SAT do motor: restrições de carreta e de capacidade por bucket, com o objetivo em modo
// Ranking — pesos derivados da ordem de importância do setup vinculado ao cenário (ver Objetivo.cs).
// Os itens já chegam com o volume pinado descontado (ver OtimizadorService.DescontarPinados).
//
// Cada item (= cliente+produto agrupado, ver Preparacao.cs) é indivisível (um único bucket,
// tudo-ou-nada) quando seu piso é igual ao volume total, ou divisível (pode se espalhar por vários
// buckets simultaneamente, nunca abaixo do piso em cada um) quando o piso é menor que o volume —
// mesmo modelo do projeto de referência otimizador-teste-entrega.
public static class Otimizacao
{
    public const int Escala = 10;

    private static long Escalar(double m3) => (long)Math.Round(m3 * Escala);

    public static ResultadoOtimizacao Resolver(
        Config config,
        IReadOnlyList<Item> itens,
        CapacidadeHorizonte capacidade,
        IReadOnlyList<TermoObjetivo> termos,
        List<string> notas)
    {
        var semanas = capacidade.Semanas.Count;
        var itensPorIndice = itens.ToDictionary(i => i.Indice);
        var prioridadeAntiguidade = CalcularPrioridadeAntiguidade(itens);

        var pesos = termos.ToDictionary(t => t.Criterio, t => t.Peso);
        // "atender" nunca fica com peso 0, mesmo que o critério esteja inativo/ausente no setup — sem
        // isso o solver pode descartar demanda de propósito com capacidade sobrando, porque nada no
        // objetivo custaria isso (mesma armadilha documentada no projeto de referência,
        // Modelo/Objetivo.cs de lá — Diagnosticar).
        var pesoAtender = Math.Max(pesos.GetValueOrDefault(CriterioOrdemEnum.AtenderDemanda), 1);
        var pesoAntiguidade = pesos.GetValueOrDefault(CriterioOrdemEnum.PedidoMaisAntigo);
        var pesoIndustria = pesos.GetValueOrDefault(CriterioOrdemEnum.PiorizarClienteRevenda);
        var pesoAtraso = pesos.GetValueOrDefault(CriterioOrdemEnum.Antecipar);
        var pesoMixFrete = pesos.GetValueOrDefault(CriterioOrdemEnum.PriorizarFreteCIF);

        var custoNaoAtender = itens.ToDictionary(
            it => it.Indice,
            it => pesoAtender
                  + pesoAntiguidade * prioridadeAntiguidade.GetValueOrDefault(it.Indice, 1)
                  + (it.Industria ? pesoIndustria : 0));

        var modelo = new CpModel();

        var y = new Dictionary<(int, int, int), BoolVar>();
        var x = new Dictionary<(int, int, int), IntVar>();
        var slack = new Dictionary<int, IntVar>();
        var contribuicao = new Dictionary<(int, int, int), LinearExpr>();
        var itensAtivos = 0;
        var itensDivisiveis = 0;

        foreach (var item in itens)
        {
            var volume = Escalar(item.VolumeM3);
            if (volume <= 0) continue; // volume já 100% coberto por pin — fora do modelo

            itensAtivos++;
            var i = item.Indice;
            var piso = Escalar(item.Piso);
            var divisivel = piso < volume;
            if (divisivel) itensDivisiveis++;

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

            var contribuicoesItem = new List<LinearExpr>();
            var escolhas = new List<BoolVar>();

            foreach (var (centro, s) in baldes)
            {
                var vy = modelo.NewBoolVar($"y_{i}_{centro}_{s}");
                y[(i, centro, s)] = vy;
                escolhas.Add(vy);

                LinearExpr contrib;
                if (divisivel)
                {
                    var vx = modelo.NewIntVar(0, volume, $"x_{i}_{centro}_{s}");
                    modelo.Add(vx <= vy * volume);
                    modelo.Add(vx >= vy * piso);
                    x[(i, centro, s)] = vx;
                    contrib = vx;
                }
                else
                {
                    contrib = LinearExpr.Term(vy, volume);
                }

                contribuicao[(i, centro, s)] = contrib;
                contribuicoesItem.Add(contrib);
            }

            if (!divisivel)
                modelo.Add(LinearExpr.Sum(escolhas) <= 1);

            modelo.Add(LinearExpr.Sum(contribuicoesItem) + slack[i] == volume);
        }

        notas.Add($"modelo: {itensAtivos} item(ns) — {itensDivisiveis} divisível(eis) entre vários "
                  + $"buckets (centro, semana) respeitando o piso, {itensAtivos - itensDivisiveis} "
                  + "indivisível(eis) (tudo-ou-nada)");

        // Restrição de capacidade: o volume total alocado em cada bucket (centro, linha produto,
        // semana) nunca pode ultrapassar a capacidade declarada nele.
        var contribuicaoPorBucketCapacidade = new Dictionary<(int Centro, int LinhaProduto, int Semana), List<LinearExpr>>();
        foreach (var ((i, centro, s), expr) in contribuicao)
        {
            var chave = (centro, itensPorIndice[i].LinhaProdutoId, s);
            if (!contribuicaoPorBucketCapacidade.TryGetValue(chave, out var lista))
                contribuicaoPorBucketCapacidade[chave] = lista = [];
            lista.Add(expr);
        }

        foreach (var (chave, parcelas) in contribuicaoPorBucketCapacidade)
        {
            var disponivel = Escalar(capacidade.Disponivel(chave.Centro, chave.LinhaProduto, chave.Semana));
            modelo.Add(LinearExpr.Sum(parcelas) <= disponivel);
        }

        notas.Add($"capacidade: {contribuicaoPorBucketCapacidade.Count} bucket(s) (centro, linha "
                  + "produto, semana) com teto de volume alocado");

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

        // Objetivo, modo Ranking — soma dos termos ativos do setup, cada um ponderado por
        // Objetivo.CalcularPesos (BaseRanking^(N-ordem)).
        var objetivoTermos = new List<LinearExpr> { LinearExpr.Sum(slack.Values) * pesoAtender };

        if (pesoAntiguidade > 0 && slack.Count > 0)
            objetivoTermos.Add(LinearExpr.Sum(slack.Select(kv =>
                kv.Value * (pesoAntiguidade * prioridadeAntiguidade.GetValueOrDefault(kv.Key, 1)))));

        if (pesoIndustria > 0)
        {
            var slackIndustria = slack
                .Where(kv => itensPorIndice[kv.Key].Industria)
                .Select(kv => (LinearExpr)kv.Value)
                .ToList();

            if (slackIndustria.Count > 0)
                objetivoTermos.Add(LinearExpr.Sum(slackIndustria) * pesoIndustria);
        }

        if (pesoAtraso > 0 && contribuicao.Count > 0)
            objetivoTermos.Add(LinearExpr.Sum(contribuicao.Select(kv => kv.Value * (long)kv.Key.Item3)) * pesoAtraso);

        var mixFreteAtivo = config.MixFrete.Ativo && pesoMixFrete > 0 && contribuicao.Count > 0;
        IntVar? mixFreteBaixo = null;
        IntVar? mixFreteAlto = null;

        if (mixFreteAtivo)
        {
            // Desvio global (não por bucket, por simplicidade) entre o volume CIF alocado no
            // horizonte inteiro e o alvo do setup — folgaBaixo/folgaAlto aproximam |CIF - alvo*total|
            // em décimos de m³ (escala ×1000 só na comparação, para manter o coeficiente do alvo
            // inteiro sem perder precisão).
            var alvoCifMilesimos = (long)Math.Round(config.MixFrete.AlvoCif * 1000);
            var volumeCifExpr = LinearExpr.Sum(contribuicao
                .Where(kv => itensPorIndice[kv.Key.Item1].Cif)
                .Select(kv => kv.Value));
            var volumeAlocadoExpr = LinearExpr.Sum(contribuicao.Values);
            var tetoDesvio = Escalar(itens.Sum(it => it.VolumeM3));

            mixFreteBaixo = modelo.NewIntVar(0, tetoDesvio, "mixFreteBaixo");
            mixFreteAlto = modelo.NewIntVar(0, tetoDesvio, "mixFreteAlto");

            modelo.Add(mixFreteAlto * 1000 >= volumeCifExpr * 1000 - volumeAlocadoExpr * alvoCifMilesimos);
            modelo.Add(mixFreteBaixo * 1000 >= volumeAlocadoExpr * alvoCifMilesimos - volumeCifExpr * 1000);

            objetivoTermos.Add((mixFreteBaixo + mixFreteAlto) * pesoMixFrete);

            notas.Add($"mix de frete: alvo {config.MixFrete.AlvoCif:P0} CIF sobre o volume alocado no "
                      + $"horizonte, peso {pesoMixFrete}");
        }

        // ------------------------------------------------------------------- hint
        // Solução inicial gulosa (Greedy.Alocar) usada como hint do CP-SAT — sem incumbente o
        // solver gasta o orçamento inteiro procurando a primeira solução decente em vez de
        // melhorar uma já razoável (ver Greedy.cs). Reusa custoNaoAtender (já calculado acima)
        // como prioridade de alocação: é o mesmo peso composto (atender + antiguidade +
        // indústria) que o objetivo usa para penalizar slack, então guia o greedy na mesma
        // direção que o solver otimiza.
        //
        // TODA variável do modelo precisa de valor no hint: hint incompleto é descartado
        // inteiro, em silêncio — por isso a contagem de completude no fim deste bloco.
        var greedy = Greedy.Alocar(itens, custoNaoAtender, capacidade, semanas, config, Escalar);
        var hint = greedy.Alocacao;

        var problemasGreedy = Greedy.Validar(greedy, itens, capacidade, config, Escalar);
        if (problemasGreedy.Count > 0)
            notas.Add($"ATENÇÃO greedy infactível em {problemasGreedy.Count} ponto(s) — hint será "
                      + $"descartado pelo solver. Primeiros: {string.Join(" | ", problemasGreedy.Take(3))}");

        var hintVars = new List<IntVar>();
        var hintValores = new List<long>();

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

        foreach (var (i, slackVar) in slack)
        {
            var alocadoItem = hint.Where(kv => kv.Key.Item == i).Sum(kv => kv.Value);
            hintVars.Add(slackVar);
            hintValores.Add(Math.Max(0, Escalar(itensPorIndice[i].VolumeM3) - alocadoItem));
        }

        foreach (var (chave, n) in carretas)
        {
            var quantas = greedy.Carretas.GetValueOrDefault(chave, 0);
            hintVars.Add(n);
            hintValores.Add(quantas);
            hintVars.Add(embarques[chave]);
            hintValores.Add(quantas > 0 ? 1 : 0);
        }

        if (mixFreteAtivo)
        {
            var alvoCifMilesimosHint = (long)Math.Round(config.MixFrete.AlvoCif * 1000);
            var cifGreedy = hint.Where(kv => itensPorIndice[kv.Key.Item].Cif).Sum(kv => kv.Value);
            var totalGreedy = hint.Values.Sum();

            hintVars.Add(mixFreteAlto!);
            hintValores.Add((long)Math.Max(0,
                Math.Ceiling((cifGreedy * 1000 - totalGreedy * alvoCifMilesimosHint) / 1000.0)));
            hintVars.Add(mixFreteBaixo!);
            hintValores.Add((long)Math.Max(0,
                Math.Ceiling((totalGreedy * alvoCifMilesimosHint - cifGreedy * 1000) / 1000.0)));
        }

        // Contagem pelo PROTO do modelo, não por aritmética manual: se uma variável nova for
        // criada e esquecida na soma acima, a salvaguarda mentiria — e hint incompleto é
        // descartado inteiro, em silêncio.
        var variaveisDoModelo = modelo.Model.Variables.Count;
        if (hintVars.Count < variaveisDoModelo)
            notas.Add($"ATENÇÃO hint incompleto: {hintVars.Count} de {variaveisDoModelo} "
                      + "variáveis — o CP-SAT vai descartar o hint inteiro");

        for (var h = 0; h < hintVars.Count; h++)
            modelo.AddHint(hintVars[h], hintValores[h]);

        var volumeHintM3 = greedy.VolumeAlocado / (double)Escala;
        notas.Add($"greedy inicial: {volumeHintM3:N0} m3 alocados "
                  + $"({volumeHintM3 / Math.Max(itens.Sum(it => it.VolumeM3), 1):P1} do elegível) — usado como hint");

        modelo.Minimize(LinearExpr.Sum(objetivoTermos));

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
                var vAlocado = x.TryGetValue((i, centro, s), out var vx)
                    ? solver.Value(vx)
                    : Escalar(item.VolumeM3);
                if (vAlocado <= 0) continue;

                var motivos = ComputarMotivosSemana(item, centro, s, capacidade)
                    .Select(m => new MotivoAlocacao(CategoriaMotivoEnum.PorqueNestaSemana, m))
                    .Append(new MotivoAlocacao(
                        CategoriaMotivoEnum.PorqueNesteCentro, ComputarMotivoCentro(item, capacidade, semanas)))
                    .ToList();

                alocacoes.Add(new Alocacao(
                    i, centro, s, vAlocado / (double)Escala, (int)custoNaoAtender[i], motivos));
            }

            foreach (var (i, var_) in slack)
            {
                var v = solver.Value(var_);
                if (v <= 0) continue;

                var volumeM3 = v / (double)Escala;
                naoAlocado[i] = volumeM3;

                motivoNaoAlocado[i] = config.Carreta.Ativa && volumeM3 < config.Carreta.MinimoM3
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

        var variaveis = slack.Count + carretas.Count + x.Count + (mixFreteAtivo ? 2 : 0);
        var binarias = y.Count + embarques.Count;

        return new ResultadoOtimizacao(
            status.ToString(),
            solver.WallTime(),
            status is CpSolverStatus.Optimal or CpSolverStatus.Feasible ? solver.ObjectiveValue : double.NaN,
            alocacoes.OrderBy(a => a.IndiceSemana).ThenBy(a => a.CentroId).ToList(),
            naoAlocado,
            motivoNaoAlocado,
            variaveis,
            binarias,
            listaEmbarques.OrderByDescending(e => e.VolumeM3).ToList());
    }

    // Prioridade 1..20 por percentil de antiguidade (mais antigo = maior prioridade). Percentil em
    // vez de normalização linear da data: a carteira tende a concentrar a maior parte das linhas nos
    // últimos meses, e uma normalização linear "achataria" o efeito da antiguidade para quase zero na
    // maioria dos itens (mesma técnica e razão documentadas no projeto de referência
    // otimizador-teste-entrega).
    private static Dictionary<int, long> CalcularPrioridadeAntiguidade(IReadOnlyList<Item> itens)
    {
        var ordenados = itens.OrderBy(i => i.DataDocumentoMaisAntiga).ToList();
        var n = ordenados.Count;
        var resultado = new Dictionary<int, long>();

        for (var pos = 0; pos < n; pos++)
        {
            var percentil = n > 1 ? 1.0 - (double)pos / (n - 1) : 1.0;
            resultado[ordenados[pos].Indice] = (long)Math.Round(1 + percentil * 19);
        }

        return resultado;
    }

    // "Porque nesta semana": olha, no centro escolhido, todas as semanas anteriores à escolhida —
    // se em alguma delas não havia capacidade nenhuma, ou havia capacidade mas insuficiente para o
    // piso do item (o menor fragmento possível), isso explica por que o item não pôde ser alocado
    // antes. É heurístico (o CP-SAT não expõe uma "razão" nativa para a escolha entre buckets
    // empatados), não um traço literal do solver.
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
            else if (disponivel < item.Piso && !motivos.Contains(MotivoAlocacaoEnum.LoteMinimoNaoCabeAntes))
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
