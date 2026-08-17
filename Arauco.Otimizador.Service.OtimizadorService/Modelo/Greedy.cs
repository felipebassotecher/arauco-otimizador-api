using Arauco.Otimizador.Service.OtimizadorService.Capacidade;
using Arauco.Otimizador.Service.OtimizadorService.Dados;

namespace Arauco.Otimizador.Service.OtimizadorService.Modelo;

/// <summary>Solução inicial: alocações + quantas carretas por embarque.</summary>
public sealed record SolucaoGreedy(
    Dictionary<(int Item, int Centro, int Semana), long> Alocacao,
    Dictionary<(string Cliente, int Centro, int Semana), int> Carretas)
{
    public long VolumeAlocado => Alocacao.Values.Sum();
}

/// <summary>
/// Alocação gulosa usada como SOLUÇÃO INICIAL (hint) do CP-SAT em Otimizacao.Resolver.
///
/// Por que existe: sem incumbente o CP-SAT gasta o orçamento inteiro procurando a primeira
/// solução decente. Com um hint razoável, o CP-SAT passa a MELHORAR uma solução em vez de
/// procurar a primeira — ver Otimizacao.cs (bloco "hint" antes do solver.Solve).
///
/// Consolida POR CLIENTE quando a carreta está ligada: com carreta ativa, alocar um item
/// isolado quase sempre viola a carga mínima do embarque, o hint sai infactível e o CP-SAT o
/// descarta EM SILÊNCIO — devolvendo resultado pior que o próprio greedy (medido no projeto de
/// referência: 108.519 -> 39.574 m3).
///
/// Trabalha nas MESMAS unidades inteiras do modelo (décimos de m3, via Otimizacao.Escalar).
/// Fazer o greedy em double e arredondar depois também produz hint infactível, porque a soma
/// das parcelas arredondadas não fecha com o volume arredondado do item.
/// </summary>
public static class Greedy
{
    /// <summary>
    /// Teto de carretas de um embarque. Usado pelo MODELO (domínio da variável) e pelo GREEDY
    /// (hint) — se divergirem, o hint sai fora de domínio e é descartado inteiro, em silêncio.
    /// </summary>
    public static int TetoCarretas(double volumeCliente, Config config) =>
        Math.Clamp(
            (int)Math.Ceiling(volumeCliente / config.Carreta.MaximoM3),
            1, config.Carreta.MaximoCarretasPorEmbarque);

    public static SolucaoGreedy Alocar(
        IReadOnlyList<Item> itens,
        IReadOnlyDictionary<int, long> prioridades,
        CapacidadeHorizonte capacidade,
        int semanas,
        Config config,
        Func<double, long> escalar)
    {
        var restante = new Dictionary<(int, int, int), long>();
        foreach (var (chave, valor) in capacidade.PorBucket)
            restante[(chave.Centro, chave.LinhaProduto, chave.IndiceSemana)] = escalar(valor);

        var alocacao = new Dictionary<(int, int, int), long>();
        var carretas = new Dictionary<(string, int, int), int>();

        if (!config.Carreta.Ativa)
        {
            AlocarSemCarreta(itens, prioridades, semanas, escalar, restante, alocacao);
            return new SolucaoGreedy(alocacao, carretas);
        }

        var minCarreta = escalar(config.Carreta.MinimoM3);
        var maxCarreta = escalar(config.Carreta.MaximoM3);

        // Cliente inteiro de uma vez: a carreta só fecha somando os SKUs dele.
        // Ordem: cliente cuja demanda tem maior prioridade média ponderada por volume.
        var porCliente = itens
            .GroupBy(i => i.ClienteId)
            .Select(g => new
            {
                Cliente = g.Key,
                Itens = g.ToList(),
                Peso = g.Sum(i => prioridades[i.Indice] * i.VolumeM3) / Math.Max(g.Sum(i => i.VolumeM3), 1e-9),
            })
            .OrderByDescending(c => c.Peso)
            .ThenByDescending(c => c.Itens.Sum(i => i.VolumeM3));

        foreach (var cliente in porCliente)
        {
            var falta = cliente.Itens.ToDictionary(i => i.Indice, i => escalar(i.VolumeM3));

            // Carretas que este cliente já recebeu em cada semana, somando as plantas. Sem
            // isso o greedy estoura o teto de recebimento — hint infactível, descartado
            // INTEIRO, em silêncio, devolvendo resultado pior que o greedy.
            var recebidasNaSemana = new Dictionary<int, int>();
            var limiteCliente = config.LimiteRecebimento.Ativo
                ? config.LimiteRecebimento.De(cliente.Cliente)
                : int.MaxValue;

            // Semana mais cedo primeiro (o objetivo penaliza semana tardia).
            var baldes = Enumerable.Range(0, semanas)
                .SelectMany(s => cliente.Itens.SelectMany(i => i.CentrosElegiveis).Distinct().Select(c => (Centro: c, Semana: s)))
                .Distinct()
                .OrderBy(b => b.Semana).ThenBy(b => b.Centro);

            foreach (var (centro, s) in baldes)
            {
                if (falta.Values.Sum() <= 0) break;

                // Quantas carretas ainda cabem no teto de recebimento desta semana.
                var folgaRecebimento = limiteCliente - recebidasNaSemana.GetValueOrDefault(s);
                if (folgaRecebimento <= 0) continue;

                // Monta um embarque candidato: quanto de cada SKU cabe neste balde.
                var candidato = new List<(int Item, long Volume)>();
                var consumo = new Dictionary<(int, int, int), long>();

                foreach (var item in cliente.Itens.OrderByDescending(i => prioridades[i.Indice]))
                {
                    var pendente = falta[item.Indice];
                    if (pendente <= 0) continue;
                    if (!item.CentrosElegiveis.Contains(centro)) continue;

                    var chaveCap = (centro, item.LinhaProdutoId, s);
                    var disponivel = restante.GetValueOrDefault(chaveCap, 0) - consumo.GetValueOrDefault(chaveCap, 0);
                    if (disponivel <= 0) continue;

                    var piso = escalar(item.Piso);
                    var quanto = Math.Min(pendente, disponivel);
                    if (quanto < piso) continue;                 // SKU pingado: não entra

                    // Não deixar resto do item abaixo do piso — ele não embarcaria depois.
                    var resto = pendente - quanto;
                    if (resto > 0 && resto < piso)
                    {
                        quanto = disponivel >= pendente ? pendente : pendente - piso;
                        if (quanto < piso) continue;
                    }

                    candidato.Add((item.Indice, quanto));
                    consumo[chaveCap] = consumo.GetValueOrDefault(chaveCap) + quanto;
                }

                var total = candidato.Sum(c => c.Volume);
                if (total < minCarreta) continue;                 // não fecha nem uma carreta

                // Menor número de carretas que comporta o volume. Tem que ser
                // ceil(total/max) e NÃO floor(total/min): o domínio de `n` no modelo é
                // [0, ceil(volumeDoCliente/max)], e floor(total/min) pode estourar esse teto —
                // hint fora de domínio e descartado inteiro.
                var n = (int)Math.Ceiling(total / (double)maxCarreta);

                // Dois tetos distintos, aplicados juntos: o domínio da variável no MODELO (por
                // cliente inteiro) e o que resta do limite de recebimento (por cliente x
                // semana). Estourar o primeiro põe o hint fora de domínio; estourar o segundo
                // o põe fora da restrição. Os dois descartam o hint inteiro.
                var teto = Math.Min(
                    TetoCarretas(cliente.Itens.Sum(i => i.VolumeM3), config),
                    folgaRecebimento);

                if (n > teto)
                {
                    n = teto;
                    total = Aparar(candidato, itens, escalar, n * maxCarreta);
                }

                // Se o volume cai no vazio entre n*min e n*max, desce uma carreta e apara o
                // excesso até o teto dela.
                if (total < n * minCarreta)
                {
                    n--;
                    if (n <= 0) continue;
                    total = Aparar(candidato, itens, escalar, n * maxCarreta);
                    if (total < n * minCarreta) continue;
                }

                foreach (var (indice, volume) in candidato)
                {
                    if (volume <= 0) continue;
                    var chave = (indice, centro, s);
                    alocacao[chave] = alocacao.GetValueOrDefault(chave) + volume;
                    falta[indice] -= volume;

                    // Índice é a posição na lista, por construção (Preparador).
                    var item = itens[indice];
                    var chaveCap = (centro, item.LinhaProdutoId, s);
                    restante[chaveCap] = restante.GetValueOrDefault(chaveCap) - volume;
                }

                carretas[(cliente.Cliente, centro, s)] = n;
                recebidasNaSemana[s] = recebidasNaSemana.GetValueOrDefault(s) + n;
            }
        }

        return new SolucaoGreedy(alocacao, carretas);
    }

    /// <summary>
    /// Confere a solução gulosa contra as restrições do modelo ANTES de virar hint.
    ///
    /// Existe porque o CP-SAT descarta hint infactível em SILÊNCIO: ele reporta uma linha no
    /// log e segue como se não houvesse hint, e o resultado piora sem nenhum erro. Melhor
    /// gastar alguns milissegundos conferindo e falar alto (via `notas`).
    /// </summary>
    public static List<string> Validar(
        SolucaoGreedy solucao, IReadOnlyList<Item> itens,
        CapacidadeHorizonte capacidade, Config config, Func<double, long> escalar)
    {
        var problemas = new List<string>();
        var porIndice = itens.ToDictionary(i => i.Indice);

        // 1. Nenhum item alocado além do próprio volume.
        foreach (var g in solucao.Alocacao.GroupBy(kv => kv.Key.Item))
        {
            var soma = g.Sum(kv => kv.Value);
            var volume = escalar(porIndice[g.Key].VolumeM3);
            if (soma > volume)
                problemas.Add($"item {g.Key}: alocado {soma} > volume {volume}");
        }

        // 2. Piso por SKU respeitado.
        foreach (var ((i, centro, s), v) in solucao.Alocacao)
        {
            var piso = escalar(porIndice[i].Piso);
            if (v > 0 && v < piso)
                problemas.Add($"item {i} em ({centro},{s}): {v} abaixo do piso {piso}");
        }

        // 3. Capacidade por (centro, linha de produto, semana).
        var uso = new Dictionary<(int, int, int), long>();
        foreach (var ((i, centro, s), v) in solucao.Alocacao)
        {
            var chave = (centro, porIndice[i].LinhaProdutoId, s);
            uso[chave] = uso.GetValueOrDefault(chave) + v;
        }
        foreach (var (chave, v) in uso)
        {
            var disponivel = escalar(capacidade.Disponivel(chave.Item1, chave.Item2, chave.Item3));
            if (v > disponivel)
                problemas.Add($"capacidade ({chave.Item1},lp{chave.Item2},s{chave.Item3}): {v} > {disponivel}");
        }

        // 4. Carga fechada: volume do embarque entre n*min e n*max.
        if (config.Carreta.Ativa)
        {
            var minC = escalar(config.Carreta.MinimoM3);
            var maxC = escalar(config.Carreta.MaximoM3);

            var porEmbarque = new Dictionary<(string, int, int), long>();
            foreach (var ((i, centro, s), v) in solucao.Alocacao)
            {
                var chave = (porIndice[i].ClienteId, centro, s);
                porEmbarque[chave] = porEmbarque.GetValueOrDefault(chave) + v;
            }

            var volumePorCliente = itens.GroupBy(i => i.ClienteId)
                .ToDictionary(g => g.Key, g => g.Sum(i => i.VolumeM3));

            foreach (var (chave, v) in porEmbarque)
            {
                var n = solucao.Carretas.GetValueOrDefault(chave, 0);
                if (n == 0 && v > 0) problemas.Add($"embarque {chave}: volume {v} sem carreta");
                else if (n > 0 && (v < n * minC || v > n * maxC))
                    problemas.Add($"embarque {chave}: volume {v} fora de [{n * minC}, {n * maxC}]");

                // Fora de domínio é a falha que mais custa: o CP-SAT rejeita o hint INTEIRO
                // por causa de uma variável.
                var teto = TetoCarretas(volumePorCliente.GetValueOrDefault(chave.Item1), config);
                if (n > teto)
                    problemas.Add($"embarque {chave}: n={n} acima do teto de domínio {teto}");
            }
        }

        // 5. Limite de recebimento: carretas do cliente somadas sobre as plantas, por semana.
        // Sem esta checagem a salvaguarda do hint teria um buraco justamente aqui — a
        // restrição não cria variável nova (reusa `n`), então a contagem de completude
        // continuaria verde com o hint sendo descartado por infactibilidade.
        if (config.LimiteRecebimento.Ativo && config.Carreta.Ativa)
        {
            foreach (var g in solucao.Carretas.GroupBy(kv => (kv.Key.Cliente, kv.Key.Semana)))
            {
                var soma = g.Sum(kv => kv.Value);
                var limite = config.LimiteRecebimento.De(g.Key.Cliente);
                if (soma > limite)
                    problemas.Add($"recebimento (cliente {g.Key.Cliente}, s{g.Key.Semana}): "
                                  + $"{soma} carretas > limite {limite}");
            }
        }

        return problemas;
    }

    /// <summary>
    /// Reduz o embarque até `limite`, tirando dos SKUs de menor prioridade primeiro e nunca
    /// deixando um SKU abaixo do próprio piso (isso violaria x >= piso*y).
    /// </summary>
    private static long Aparar(
        List<(int Item, long Volume)> candidato, IReadOnlyList<Item> itens,
        Func<double, long> escalar, long limite)
    {
        var total = candidato.Sum(c => c.Volume);

        for (var k = candidato.Count - 1; k >= 0 && total > limite; k--)
        {
            var (indice, volume) = candidato[k];
            var piso = escalar(itens[indice].Piso);
            var excesso = total - limite;

            if (volume - excesso >= piso)
            {
                candidato[k] = (indice, volume - excesso);
                total -= excesso;
            }
            else
            {
                candidato[k] = (indice, 0);
                total -= volume;
            }
        }

        candidato.RemoveAll(c => c.Volume <= 0);
        return candidato.Sum(c => c.Volume);
    }

    /// <summary>Caminho sem carreta ligada: piso por item, sem consolidação por cliente.</summary>
    private static void AlocarSemCarreta(
        IReadOnlyList<Item> itens, IReadOnlyDictionary<int, long> prioridades, int semanas,
        Func<double, long> escalar,
        Dictionary<(int, int, int), long> restante,
        Dictionary<(int, int, int), long> alocacao)
    {
        var ordem = itens
            .OrderByDescending(i => prioridades[i.Indice])
            .ThenByDescending(i => i.VolumeM3);

        foreach (var item in ordem)
        {
            var volume = escalar(item.VolumeM3);
            var falta = volume;
            var piso = escalar(item.Piso);
            var indivisivel = piso >= volume;

            var baldes = Enumerable.Range(0, semanas)
                .SelectMany(s => item.CentrosElegiveis.Select(c => (Centro: c, Semana: s)))
                .OrderBy(b => b.Semana)
                .ThenByDescending(b => restante.GetValueOrDefault((b.Centro, item.LinhaProdutoId, b.Semana), 0));

            foreach (var (centro, s) in baldes)
            {
                if (falta <= 0) break;

                var chave = (centro, item.LinhaProdutoId, s);
                var disponivel = restante.GetValueOrDefault(chave, 0);
                if (disponivel <= 0) continue;

                if (indivisivel)
                {
                    if (disponivel < volume) continue;
                    alocacao[(item.Indice, centro, s)] = volume;
                    restante[chave] = disponivel - volume;
                    falta = 0;
                    break;
                }

                var quanto = Math.Min(falta, disponivel);
                if (quanto < piso) continue;

                var resto = falta - quanto;
                if (resto > 0 && resto < piso)
                {
                    quanto = disponivel >= falta ? falta : falta - piso;
                    if (quanto < piso || quanto > disponivel) continue;
                }

                alocacao[(item.Indice, centro, s)] = quanto;
                restante[chave] = disponivel - quanto;
                falta -= quanto;
            }
        }
    }
}
