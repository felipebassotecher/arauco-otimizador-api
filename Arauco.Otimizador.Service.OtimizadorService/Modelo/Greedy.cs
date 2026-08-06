using Arauco.Otimizador.Service.OtimizadorService.Capacidade;
using Arauco.Otimizador.Service.OtimizadorService.Dados;

namespace Arauco.Otimizador.Service.OtimizadorService.Modelo;

public sealed record SolucaoGreedy(
    Dictionary<(int Item, int Centro, int Semana), long> Alocacao,
    Dictionary<(string Cliente, int Centro, int Semana), int> Carretas)
{
    public long VolumeAlocado => Alocacao.Values.Sum();
}

public static class Greedy
{
    public static int TetoCarretas(double volumeCliente, Config config) =>
        Math.Clamp(
            (int)Math.Ceiling(volumeCliente / config.Carreta.MaximoM3),
            1, config.Carreta.MaximoCarretasPorEmbarque);

    public static SolucaoGreedy Alocar(
        IReadOnlyList<Item> itens,
        IReadOnlyList<long> prioridades,
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

            var recebidasNaSemana = new Dictionary<int, int>();
            var limiteCliente = config.LimiteRecebimento.Ativo
                ? config.LimiteRecebimento.De(cliente.Cliente)
                : int.MaxValue;

            var baldes = Enumerable.Range(0, semanas)
                .SelectMany(s => cliente.Itens.SelectMany(i => i.CentrosElegiveis).Distinct().Select(c => (Centro: c, Semana: s)))
                .Distinct()
                .OrderBy(b => b.Semana).ThenBy(b => b.Centro);

            foreach (var (centro, s) in baldes)
            {
                if (falta.Values.Sum() <= 0) break;

                var folgaRecebimento = limiteCliente - recebidasNaSemana.GetValueOrDefault(s);
                if (folgaRecebimento <= 0) continue;

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

                    var piso = escalar(item.LoteMinimoM3);
                    var quanto = Math.Min(pendente, disponivel);
                    if (quanto < piso) continue;

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
                if (total < minCarreta) continue;

                var n = (int)Math.Ceiling(total / (double)maxCarreta);

                var teto = Math.Min(
                    TetoCarretas(cliente.Itens.Sum(i => i.VolumeM3), config),
                    folgaRecebimento);

                if (n > teto)
                {
                    n = teto;
                    total = Aparar(candidato, itens, escalar, n * maxCarreta);
                }

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

    public static List<string> Validar(
        SolucaoGreedy solucao, IReadOnlyList<Item> itens,
        CapacidadeHorizonte capacidade, Config config, Func<double, long> escalar)
    {
        var problemas = new List<string>();
        var porIndice = itens.ToDictionary(i => i.Indice);

        foreach (var g in solucao.Alocacao.GroupBy(kv => kv.Key.Item))
        {
            var soma = g.Sum(kv => kv.Value);
            var volume = escalar(porIndice[g.Key].VolumeM3);
            if (soma > volume)
                problemas.Add($"item {g.Key}: alocado {soma} > volume {volume}");
        }

        foreach (var ((i, centro, s), v) in solucao.Alocacao)
        {
            var piso = escalar(porIndice[i].LoteMinimoM3);
            if (v > 0 && v < piso)
                problemas.Add($"item {i} em ({centro},{s}): {v} abaixo do piso {piso}");
        }

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

                var teto = TetoCarretas(volumePorCliente.GetValueOrDefault(chave.Item1), config);
                if (n > teto)
                    problemas.Add($"embarque {chave}: n={n} acima do teto de domínio {teto}");
            }
        }

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

    private static long Aparar(
        List<(int Item, long Volume)> candidato, IReadOnlyList<Item> itens,
        Func<double, long> escalar, long limite)
    {
        var total = candidato.Sum(c => c.Volume);

        for (var k = candidato.Count - 1; k >= 0 && total > limite; k--)
        {
            var (indice, volume) = candidato[k];
            var piso = escalar(itens[indice].LoteMinimoM3);
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

    private static void AlocarSemCarreta(
        IReadOnlyList<Item> itens, IReadOnlyList<long> prioridades, int semanas,
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
            var piso = escalar(item.LoteMinimoM3);
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
