namespace Arauco.Otimizador.Service.OtimizadorService.Dados;

public sealed class Carregador
{
    public IReadOnlyList<LinhaCarteira> Carteira { get; private init; } = [];
    public IReadOnlyDictionary<string, Produto> Produtos { get; private init; } = new Dictionary<string, Produto>();
    public IReadOnlyList<Centro> Centros { get; private init; } = [];
    public IReadOnlyDictionary<string, IReadOnlyList<int>> Elegibilidade { get; private init; }
        = new Dictionary<string, IReadOnlyList<int>>();
    public IReadOnlyList<CapacidadeSemanal> Capacidade { get; private init; } = [];
    public IReadOnlyCollection<(int Centro, int LinhaProduto)> ParesElegiveis { get; private init; } = [];
    public List<string> Notas { get; private init; } = [];

    public static async Task<Carregador> CarregarAsync(string pasta)
    {
        var notas = new List<string>();

        var centros = await CarregarCentrosAsync(Path.Combine(pasta, "centros.parquet"), notas);
        var produtos = await CarregarProdutosAsync(Path.Combine(pasta, "produtos.parquet"));
        var elegibilidade = await CarregarElegibilidadeAsync(
            Path.Combine(pasta, "elegibilidade.parquet"), centros, notas);
        var carteira = await CarregarCarteiraAsync(Path.Combine(pasta, "demanda.parquet"), notas);
        var capacidade = await CarregarCapacidadeAsync(
            Path.Combine(pasta, "capacidade.parquet"), centros, notas);

        var paresElegiveis = elegibilidade
            .SelectMany(kv => kv.Value.Select(c =>
                (Centro: c, LinhaProduto: produtos.TryGetValue(kv.Key, out var pr) ? pr.LinhaProdutoId : -1)))
            .Where(x => x.LinhaProduto > 0)
            .Distinct()
            .ToList();

        return new Carregador
        {
            ParesElegiveis = paresElegiveis,
            Centros = centros,
            Produtos = produtos,
            Elegibilidade = elegibilidade,
            Carteira = carteira,
            Capacidade = capacidade,
            Notas = notas,
        };
    }

    public Carregador ComCarteira(IReadOnlyList<LinhaCarteira> carteira) => new()
    {
        Carteira = carteira,
        Produtos = Produtos,
        Centros = Centros,
        Elegibilidade = Elegibilidade,
        Capacidade = Capacidade,
        ParesElegiveis = ParesElegiveis,
        Notas = Notas,
    };

    private static async Task<List<Centro>> CarregarCentrosAsync(string caminho, List<string> notas)
    {
        var t = await TabelaParquet.LerAsync(caminho);
        var id = t.Inteiro("CentroId");
        var codigo = t.Texto("Codigo");
        var nome = t.Texto("Nome");
        var ativo = t.Booleano("Ativo");
        var pctInd = t.Inteiro("PorcentagemIndustria");
        var pctRev = t.Inteiro("PorcentagemRevenda");

        var todos = new List<Centro>();
        for (var i = 0; i < t.Linhas; i++)
            todos.Add(new Centro((int)(id[i] ?? 0), codigo[i] ?? "", nome[i] ?? "",
                ativo[i] ?? false, (int)(pctInd[i] ?? 0), (int)(pctRev[i] ?? 0)));

        var ativos = todos.Where(x => x.Ativo).ToList();
        var descartados = todos.Count - ativos.Count;
        if (descartados > 0)
            notas.Add($"centros: {descartados} inativo(s) descartado(s) "
                      + $"({string.Join(", ", todos.Where(x => !x.Ativo).Select(x => x.Nome))})");

        return ativos;
    }

    private static async Task<Dictionary<string, Produto>> CarregarProdutosAsync(string caminho)
    {
        var t = await TabelaParquet.LerAsync(caminho);
        var id = t.Texto("produto_id");
        var desc = t.Texto("descricao");
        var lp = t.Inteiro("linha_produto_id");
        var lote = t.Decimal("lote_minimo");
        var largura = t.Decimal("largura");
        var comprimento = t.Decimal("comprimento");
        var espessura = t.Decimal("espessura");
        var ativo = t.Booleano("ativo");

        var mapa = new Dictionary<string, Produto>(StringComparer.Ordinal);
        for (var i = 0; i < t.Linhas; i++)
        {
            if (id[i] is not { } pid) continue;
            mapa[pid] = new Produto(pid, desc[i] ?? "", (int)(lp[i] ?? 0),
                lote[i] ?? 0, largura[i] ?? 0, comprimento[i] ?? 0, espessura[i] ?? 0,
                ativo[i] ?? false);
        }
        return mapa;
    }

    private static async Task<Dictionary<string, IReadOnlyList<int>>> CarregarElegibilidadeAsync(
        string caminho, List<Centro> centros, List<string> notas)
    {
        var t = await TabelaParquet.LerAsync(caminho);
        var produto = t.Texto("produto_id");
        var centro = t.Inteiro("centro_id");

        var ativos = centros.Select(x => x.CentroId).ToHashSet();
        var mapa = new Dictionary<string, SortedSet<int>>(StringComparer.Ordinal);
        var foraDeCentroAtivo = 0;

        for (var i = 0; i < t.Linhas; i++)
        {
            if (produto[i] is not { } pid || centro[i] is not { } cid) continue;
            if (!ativos.Contains((int)cid)) { foraDeCentroAtivo++; continue; }

            if (!mapa.TryGetValue(pid, out var conjunto))
                mapa[pid] = conjunto = [];
            conjunto.Add((int)cid);
        }

        if (foraDeCentroAtivo > 0)
            notas.Add($"elegibilidade: {foraDeCentroAtivo} par(es) apontando para centro inativo, ignorados");

        return mapa.ToDictionary(kv => kv.Key, kv => (IReadOnlyList<int>)kv.Value.ToList(),
            StringComparer.Ordinal);
    }

    private static async Task<List<LinhaCarteira>> CarregarCarteiraAsync(string caminho, List<string> notas)
    {
        var t = await TabelaParquet.LerAsync(caminho);
        var carteiraId = t.Inteiro("carteira_id");
        var clienteId = t.Texto("cliente_id");
        var clienteNome = t.Texto("cliente_nome");
        var produtoId = t.Texto("produto_id");
        var linhaProduto = t.Inteiro("linha_produto_id");
        var volume = t.Decimal("volume_m3");
        var dataDoc = t.Data("data_documento");
        var incoterms = t.Texto("incoterms");
        var segmento = t.Texto("segmento");
        var centroOrig = t.Inteiro("centro_original");

        var linhas = new List<LinhaCarteira>(t.Linhas);
        var semVolume = 0;

        for (var i = 0; i < t.Linhas; i++)
        {
            var v = volume[i] ?? 0;
            if (v <= 0) { semVolume++; continue; }

            linhas.Add(new LinhaCarteira(
                carteiraId[i] ?? 0,
                clienteId[i] ?? "",
                clienteNome[i] ?? "",
                produtoId[i] ?? "",
                (int)(linhaProduto[i] ?? 0),
                v,
                dataDoc[i] ?? DateTime.MinValue,
                (incoterms[i] ?? "").Trim().ToUpperInvariant(),
                (segmento[i] ?? "").Trim(),
                centroOrig[i] ?? 0));
        }

        if (semVolume > 0)
            notas.Add($"carteira: {semVolume} linha(s) com volume <= 0, descartadas");

        return linhas;
    }

    private static async Task<List<CapacidadeSemanal>> CarregarCapacidadeAsync(
        string caminho, List<Centro> centrosAtivos, List<string> notas)
    {
        var t = await TabelaParquet.LerAsync(caminho);
        var centro = t.Inteiro("centro_id");
        var linhaProducao = t.Inteiro("linha_producao_id");
        var linhaProduto = t.Inteiro("linha_produto_id");
        var semana = t.Inteiro("semana_iso");
        var ano = t.Inteiro("ano");
        var mes = t.Inteiro("mes");
        var tipo = t.Inteiro("tipo_alocacao");
        var qtd = t.Inteiro("quantidade");
        var criacao = t.Data("data_criacao");

        var porChave = new Dictionary<(int, int, int, SemanaIso), (DateTime Criacao, long Qtd)>();
        var ativos = centrosAtivos.Select(c => c.CentroId).ToHashSet();
        var descartadosTipo = 0;
        var descartadosCentro = 0;
        var sobrepostos = 0;

        for (var i = 0; i < t.Linhas; i++)
        {
            if (tipo[i] != 1) { descartadosTipo++; continue; }
            if (!ativos.Contains((int)(centro[i] ?? 0))) { descartadosCentro++; continue; }
            if (semana[i] is not { } sem || qtd[i] is not { } q) continue;

            var anoPlano = (int)(ano[i] ?? 0);
            var mesPlano = (int)(mes[i] ?? 0);
            var anoSemana = (mesPlano >= 11 && sem <= 4) ? anoPlano + 1
                          : (mesPlano <= 2 && sem >= 49) ? anoPlano - 1
                          : anoPlano;

            var chave = ((int)(centro[i] ?? 0), (int)(linhaProducao[i] ?? 0),
                         (int)(linhaProduto[i] ?? 0), new SemanaIso(anoSemana, (int)sem));
            var quando = criacao[i] ?? DateTime.MinValue;

            if (porChave.TryGetValue(chave, out var existente))
            {
                sobrepostos++;
                if (quando <= existente.Criacao) continue;
            }
            porChave[chave] = (quando, q);
        }

        if (descartadosTipo > 0)
            notas.Add($"capacidade: {descartadosTipo} linha(s) fora de MercadoInterno, ignoradas");
        if (descartadosCentro > 0)
            notas.Add($"capacidade: {descartadosCentro} linha(s) de centro inativo, ignoradas");
        if (sobrepostos > 0)
            notas.Add($"capacidade: {sobrepostos} chave(s) em planos sobrepostos — "
                      + "mantido o planejamento mais recente");

        return porChave
            .GroupBy(kv => (Centro: kv.Key.Item1, LinhaProduto: kv.Key.Item3, Semana: kv.Key.Item4))
            .Select(g => new CapacidadeSemanal(g.Key.Centro, g.Key.LinhaProduto, g.Key.Semana,
                g.Sum(x => x.Value.Qtd)))
            .OrderBy(x => x.Semana).ThenBy(x => x.CentroId).ThenBy(x => x.LinhaProdutoId)
            .ToList();
    }
}
