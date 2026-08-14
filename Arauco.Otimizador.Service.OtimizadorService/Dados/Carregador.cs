using Arauco.Otimizador.Data.Entities;
using Microsoft.EntityFrameworkCore;

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

    public static async Task<Carregador> CarregarAsync(IUnitOfWork unitOfWork)
    {
        var notas = new List<string>();

        var centros = await CarregarCentrosAsync(unitOfWork, notas);
        var produtos = await CarregarProdutosAsync(unitOfWork);
        var elegibilidade = await CarregarElegibilidadeAsync(unitOfWork, centros, notas);
        var carteira = await CarregarCarteiraAsync(unitOfWork, notas);
        var capacidade = await CarregarCapacidadeAsync(unitOfWork, centros, notas);

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

    private static async Task<List<Centro>> CarregarCentrosAsync(IUnitOfWork unitOfWork, List<string> notas)
    {
        var linhas = await unitOfWork.CentroRepository.AsQueryable().ToListAsync();

        var todos = linhas
            .Select(l => new Centro(l.CentroId, l.Codigo ?? "", l.Nome ?? "", l.Ativo,
                l.PorcentagemIndustria, l.PorcentagemRevenda))
            .ToList();

        var ativos = todos.Where(x => x.Ativo).ToList();
        var descartados = todos.Count - ativos.Count;
        if (descartados > 0)
            notas.Add($"centros: {descartados} inativo(s) descartado(s) "
                      + $"({string.Join(", ", todos.Where(x => !x.Ativo).Select(x => x.Nome))})");

        return ativos;
    }

    private static async Task<Dictionary<string, Produto>> CarregarProdutosAsync(IUnitOfWork unitOfWork)
    {
        var linhas = await unitOfWork.ProdutoRepository.AsQueryable().ToListAsync();

        var mapa = new Dictionary<string, Produto>(StringComparer.Ordinal);
        foreach (var l in linhas)
        {
            if (l.ProdutoId is not { } pid) continue;
            mapa[pid] = new Produto(pid, l.Descricao ?? "", l.LinhaProdutoId,
                l.LoteMinimoChapas, l.LarguraMm, l.ComprimentoMm, l.EspessuraMm, l.Ativo);
        }
        return mapa;
    }

    private static async Task<Dictionary<string, IReadOnlyList<int>>> CarregarElegibilidadeAsync(
        IUnitOfWork unitOfWork, List<Centro> centros, List<string> notas)
    {
        var linhas = await unitOfWork.ElegibilidadeRepository.AsQueryable().ToListAsync();

        var ativos = centros.Select(x => x.CentroId).ToHashSet();
        var mapa = new Dictionary<string, SortedSet<int>>(StringComparer.Ordinal);
        var foraDeCentroAtivo = 0;

        foreach (var l in linhas)
        {
            if (l.ProdutoId is not { } pid) continue;
            if (!ativos.Contains(l.CentroId)) { foraDeCentroAtivo++; continue; }

            if (!mapa.TryGetValue(pid, out var conjunto))
                mapa[pid] = conjunto = [];
            conjunto.Add(l.CentroId);
        }

        if (foraDeCentroAtivo > 0)
            notas.Add($"elegibilidade: {foraDeCentroAtivo} par(es) apontando para centro inativo, ignorados");

        return mapa.ToDictionary(kv => kv.Key, kv => (IReadOnlyList<int>)kv.Value.ToList(),
            StringComparer.Ordinal);
    }

    private static async Task<List<LinhaCarteira>> CarregarCarteiraAsync(IUnitOfWork unitOfWork, List<string> notas)
    {
        var linhas = await unitOfWork.CarteiraRepository.AsQueryable().ToListAsync();

        var resultado = new List<LinhaCarteira>(linhas.Count);
        var semVolume = 0;

        foreach (var l in linhas)
        {
            var v = (double)l.VolumeM3;
            if (v <= 0) { semVolume++; continue; }

            resultado.Add(new LinhaCarteira(
                l.CarteiraId,
                l.ClienteId ?? "",
                l.ClienteNome ?? "",
                l.ProdutoId ?? "",
                l.LinhaProdutoId,
                v,
                l.DataDocumento ?? DateTime.MinValue,
                (l.Incoterms ?? "").Trim().ToUpperInvariant(),
                (l.Segmento ?? "").Trim(),
                l.CentroOriginal));
        }

        if (semVolume > 0)
            notas.Add($"carteira: {semVolume} linha(s) com volume <= 0, descartadas");

        return resultado;
    }

    private static async Task<List<CapacidadeSemanal>> CarregarCapacidadeAsync(
        IUnitOfWork unitOfWork, List<Centro> centrosAtivos, List<string> notas)
    {
        var linhas = await unitOfWork.CapacidadeRepository.AsQueryable().ToListAsync();

        var porChave = new Dictionary<(int, int, int, SemanaIso), (DateTime Criacao, long Qtd)>();
        var ativos = centrosAtivos.Select(c => c.CentroId).ToHashSet();
        var descartadosTipo = 0;
        var descartadosCentro = 0;
        var sobrepostos = 0;

        foreach (var l in linhas)
        {
            if (l.TipoAlocacao != 1) { descartadosTipo++; continue; }
            if (!ativos.Contains(l.CentroId)) { descartadosCentro++; continue; }

            var anoSemana = (l.Mes >= 11 && l.SemanaIso <= 4) ? l.Ano + 1
                          : (l.Mes <= 2 && l.SemanaIso >= 49) ? l.Ano - 1
                          : l.Ano;

            var chave = (l.CentroId, l.LinhaProducaoId, l.LinhaProdutoId, new SemanaIso(anoSemana, l.SemanaIso));
            var quando = l.DataCriacao ?? DateTime.MinValue;

            if (porChave.TryGetValue(chave, out var existente))
            {
                sobrepostos++;
                if (quando <= existente.Criacao) continue;
            }
            porChave[chave] = (quando, l.Quantidade);
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

// Carrega e cacheia o master data (produtos/centros/elegibilidade/capacidade/carteira, hoje no banco)
// em memória, evitando reconsultar o banco a cada chamada do motor de otimização.
public sealed class Executor
{
    private Carregador? _cache;
    private readonly SemaphoreSlim _trava = new(1, 1);

    public async Task<Carregador> CarregarAsync(IUnitOfWork unitOfWork)
    {
        await _trava.WaitAsync();
        try
        {
            if (_cache is not null) return _cache;
            _cache = await Carregador.CarregarAsync(unitOfWork);
            return _cache;
        }
        finally { _trava.Release(); }
    }
}
