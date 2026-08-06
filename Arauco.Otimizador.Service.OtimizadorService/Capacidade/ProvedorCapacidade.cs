using Arauco.Otimizador.Service.OtimizadorService.Dados;

namespace Arauco.Otimizador.Service.OtimizadorService.Capacidade;

public sealed record CapacidadeHorizonte(
    IReadOnlyList<SemanaIso> Semanas,
    IReadOnlyDictionary<(int Centro, int LinhaProduto, int IndiceSemana), long> PorBucket,
    double FatorAplicado,
    string Descricao)
{
    public long Total => PorBucket.Values.Sum();

    public long Disponivel(int centro, int linhaProduto, int indiceSemana) =>
        PorBucket.GetValueOrDefault((centro, linhaProduto, indiceSemana), 0L);
}

public static class ProvedorCapacidade
{
    public sealed record Bruta(
        IReadOnlyList<SemanaIso> Semanas,
        Dictionary<(int Centro, int LinhaProduto, int IndiceSemana), long> Buckets)
    {
        public IReadOnlyCollection<(int, int)> Pares =>
            Buckets.Keys.Select(k => (k.Centro, k.LinhaProduto)).Distinct().ToList();
    }

    public static Bruta MontarBruta(
        Config config, IReadOnlyList<CapacidadeSemanal> capacidadeReal,
        IReadOnlyCollection<(int Centro, int LinhaProduto)> paresElegiveis,
        List<string> notas)
    {
        var semanas = MontarSemanas(config, capacidadeReal);

        var buckets = config.Capacidade switch
        {
            ModoCapacidade.Real => MontarReal(capacidadeReal, semanas),
            ModoCapacidade.Simulada => MontarSimulada(capacidadeReal, semanas, notas),
            ModoCapacidade.Espalhada => Espalhar(
                MontarReal(capacidadeReal, semanas), paresElegiveis, config, notas),
            _ => throw new ArgumentOutOfRangeException(nameof(config)),
        };

        if (buckets.Count == 0)
            throw new InvalidOperationException(
                $"Nenhuma capacidade no horizonte {semanas[0]}..{semanas[^1]} (modo {config.Capacidade}). "
                + "Defina SemanaInicial (ex.: \"2025-W45\") ou use o modo Simulada.");

        return new Bruta(semanas, buckets);
    }

    public static CapacidadeHorizonte Aplicar(
        Config config, Bruta bruta,
        IReadOnlyDictionary<int, double> demandaPorLinhaProduto,
        List<string> notas)
    {
        var semanas = bruta.Semanas;
        var bruto = bruta.Buckets;
        var demandaTotal = demandaPorLinhaProduto.Values.Sum();

        var fatorPorLinha = new Dictionary<int, double>();
        var fatorGlobal = CalcularFator(config, bruto, demandaTotal, notas);

        if (config.AlvoCapacidadeSobreDemanda is { } alvo && config.CalibrarPorLinhaProduto)
        {
            foreach (var linha in bruto.Keys.Select(k => k.LinhaProduto).Distinct())
            {
                var capacidadeLinha = (double)bruto.Where(kv => kv.Key.LinhaProduto == linha).Sum(kv => kv.Value);
                var demandaLinha = demandaPorLinhaProduto.GetValueOrDefault(linha, 0);

                fatorPorLinha[linha] = capacidadeLinha > 0 && demandaLinha > 0
                    ? alvo * demandaLinha / capacidadeLinha
                    : 0;
            }

            var comDemanda = fatorPorLinha.Count(kv => kv.Value > 0);
            notas.Add($"capacidade: calibrada POR LINHA DE PRODUTO no alvo {alvo:0.00} "
                      + $"({comDemanda} linha(s) com demanda, "
                      + $"{fatorPorLinha.Count - comDemanda} zerada(s) por não ter demanda)");
        }

        var ajustado = new Dictionary<(int, int, int), long>(bruto.Count);
        foreach (var (chave, valor) in bruto)
        {
            var f = fatorPorLinha.TryGetValue(chave.LinhaProduto, out var porLinha) ? porLinha : fatorGlobal;

            f *= config.FatorPorCentro.GetValueOrDefault(chave.Centro, 1.0)
                 * config.FatorPorLinhaProduto.GetValueOrDefault(chave.LinhaProduto, 1.0);

            var novo = (long)Math.Round(valor * f);
            if (novo > 0) ajustado[chave] = novo;
        }

        var descricao = config.Capacidade == ModoCapacidade.Real
            ? $"real, semanas {semanas[0]}..{semanas[^1]}"
            : $"simulada (perfil do tático mais recente replicado), semanas {semanas[0]}..{semanas[^1]}";

        return new CapacidadeHorizonte(semanas, ajustado, fatorGlobal, descricao);
    }

    private static Dictionary<(int, int, int), long> Espalhar(
        Dictionary<(int, int, int), long> original,
        IReadOnlyCollection<(int Centro, int LinhaProduto)> paresElegiveis,
        Config config, List<string> notas)
    {
        var f = Math.Clamp(config.FracaoEspalhamento, 0, 1);
        if (f <= 0 || paresElegiveis.Count == 0) return original;

        var pesoCentro = original
            .GroupBy(kv => kv.Key.Item1)
            .ToDictionary(g => g.Key, g => (double)g.Sum(kv => kv.Value));
        var somaPesos = pesoCentro.Values.Sum();
        if (somaPesos <= 0) return original;

        var elegiveisPorLinha = paresElegiveis
            .GroupBy(x => x.LinhaProduto)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Centro).Distinct().ToList());

        var novo = new Dictionary<(int, int, int), long>();
        var linhasEspalhadas = 0;
        var paresNovos = 0;

        foreach (var grupo in original.GroupBy(kv => (LinhaProduto: kv.Key.Item2, Semana: kv.Key.Item3)))
        {
            var (lp, s) = grupo.Key;
            var total = grupo.Sum(kv => kv.Value);

            if (!elegiveisPorLinha.TryGetValue(lp, out var destinos) || destinos.Count <= 1)
            {
                foreach (var kv in grupo) novo[kv.Key] = kv.Value;
                continue;
            }

            linhasEspalhadas++;

            foreach (var kv in grupo)
            {
                var fica = (long)Math.Round(kv.Value * (1 - f));
                if (fica > 0) novo[kv.Key] = novo.GetValueOrDefault(kv.Key) + fica;
            }

            var migra = total - grupo.Sum(kv => (long)Math.Round(kv.Value * (1 - f)));
            var pesoDestinos = destinos.Sum(c => pesoCentro.GetValueOrDefault(c, 0));
            if (pesoDestinos <= 0) pesoDestinos = destinos.Count;

            var maiorDestino = destinos.MaxBy(c => pesoCentro.GetValueOrDefault(c, 0));
            long distribuido = 0;

            foreach (var centro in destinos)
            {
                if (centro == maiorDestino) continue;

                var peso = pesoCentro.GetValueOrDefault(centro, 1) / pesoDestinos;
                var parcela = (long)Math.Round(migra * peso);
                if (parcela <= 0) continue;

                var chave = (centro, lp, s);
                if (!original.ContainsKey(chave)) paresNovos++;
                novo[chave] = novo.GetValueOrDefault(chave) + parcela;
                distribuido += parcela;
            }

            var resto = migra - distribuido;
            if (resto > 0)
            {
                var chave = (maiorDestino, lp, s);
                if (!original.ContainsKey(chave)) paresNovos++;
                novo[chave] = novo.GetValueOrDefault(chave) + resto;
            }
        }

        notas.Add($"capacidade ESPALHADA ({f:P0} migra): {linhasEspalhadas} combinação(ões) "
                  + $"(linha de produto, semana) redistribuída(s), {paresNovos} balde(s) novo(s). "
                  + "CENÁRIO — a capacidade só foi para plantas com linha física cadastrada.");

        return novo;
    }

    private static List<SemanaIso> MontarSemanas(Config config, IReadOnlyList<CapacidadeSemanal> real)
    {
        SemanaIso inicio;

        if (!string.IsNullOrWhiteSpace(config.SemanaInicial))
        {
            var m = System.Text.RegularExpressions.Regex.Match(
                config.SemanaInicial.Trim(), @"^(\d{4})[-\s]*[Ww](\d{1,2})$");
            if (!m.Success)
                throw new ArgumentException(
                    $"SemanaInicial inválida: '{config.SemanaInicial}'. Use o formato \"2025-W45\".");
            inicio = new SemanaIso(int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value));
        }
        else if (config.Capacidade is ModoCapacidade.Real or ModoCapacidade.Espalhada)
        {
            inicio = real.Count > 0 ? real.Min(x => x.Semana) : SemanaIso.De(DateTime.Today);
        }
        else
        {
            inicio = SemanaIso.De(DateTime.Today);
        }

        return Enumerable.Range(0, config.Horizonte).Select(i => inicio.Somar(i)).ToList();
    }

    private static Dictionary<(int, int, int), long> MontarReal(
        IReadOnlyList<CapacidadeSemanal> real, List<SemanaIso> semanas)
    {
        var indice = semanas.Select((s, i) => (s, i)).ToDictionary(x => x.s, x => x.i);
        var mapa = new Dictionary<(int, int, int), long>();

        foreach (var c in real)
        {
            if (!indice.TryGetValue(c.Semana, out var i)) continue;
            var chave = (c.CentroId, c.LinhaProdutoId, i);
            mapa[chave] = mapa.GetValueOrDefault(chave) + c.Quantidade;
        }
        return mapa;
    }

    private static Dictionary<(int, int, int), long> MontarSimulada(
        IReadOnlyList<CapacidadeSemanal> real, List<SemanaIso> semanas, List<string> notas)
    {
        if (real.Count == 0)
            throw new InvalidOperationException("Sem capacidade real para derivar o perfil simulado.");

        var perfil = real
            .GroupBy(x => (x.CentroId, x.LinhaProdutoId))
            .ToDictionary(g => g.Key, g => (long)Math.Round(g.Average(x => (double)x.Quantidade)));

        notas.Add($"capacidade simulada: perfil de {perfil.Count} par(es) (centro, linha de produto) "
                  + $"replicado para {semanas.Count} semana(s)");

        var mapa = new Dictionary<(int, int, int), long>();
        for (var i = 0; i < semanas.Count; i++)
            foreach (var ((centro, linhaProduto), qtd) in perfil)
                mapa[(centro, linhaProduto, i)] = qtd;

        return mapa;
    }

    private static double CalcularFator(
        Config config, Dictionary<(int, int, int), long> bruto, double demandaElegivelM3, List<string> notas)
    {
        if (config.AlvoCapacidadeSobreDemanda is not { } alvo)
            return config.FatorCapacidade;

        var total = (double)bruto.Values.Sum();
        if (total <= 0) return config.FatorCapacidade;

        var fator = alvo * demandaElegivelM3 / total;

        notas.Add($"capacidade: alvo {alvo:0.00} x demanda elegível ({demandaElegivelM3:N0} m3) "
                  + $"=> fator {fator:0.000} sobre {total:N0} de capacidade bruta");

        if (fator > 1.0)
            notas.Add($"capacidade: ATENÇÃO — o alvo exige AUMENTAR a capacidade (fator {fator:0.00} > 1). "
                      + "A capacidade bruta já é menor que o alvo.");

        return fator;
    }
}
