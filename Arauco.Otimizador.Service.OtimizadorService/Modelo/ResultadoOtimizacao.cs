using Arauco.Otimizador.Service.OtimizadorService.Capacidade;
using Arauco.Otimizador.Service.OtimizadorService.Dados;

namespace Arauco.Otimizador.Service.OtimizadorService.Modelo;

public sealed record ResumoDto(
    double DemandaTotalM3, double DemandaElegivelM3, double ExcluidoPreflightM3,
    double AlocadoM3, double NaoAlocadoM3, long CapacidadeTotal, double FatorCapacidade,
    double PercentualAlocado, int Itens, int ItensExcluidos);

public sealed record SolverDto(
    string Status, double Segundos, double Objetivo, int Variaveis, int Binarias,
    double GreedyInicialM3);

public sealed record EmbarqueDto(
    string ClienteId, int CentroId, string Centro, string Semana, int Carretas,
    double VolumeM3, double OcupacaoMedia);

public sealed record TermoDto(string Nome, string Descricao, int Ordem, long Peso, double Valor);

public sealed record CarretasDto(
    bool Ativa, int TotalCarretas, int TotalEmbarques, double VolumeMedioPorCarreta,
    double OcupacaoMedia, double MinimoM3, double MaximoM3,
    IReadOnlyList<EmbarqueDto> Maiores, RecebimentoDto Recebimento);

public sealed record RecebimentoDto(
    bool Ativo, int CarretasPorSemana, int Excecoes,
    int ClientesNoLimite, int SemanasClienteNoLimite, int MaiorNaSemana);

public sealed record OcupacaoDto(
    int CentroId, string Centro, string Semana, int IndiceSemana,
    double AlocadoM3, double CapacidadeM3);

public sealed record AlocacaoDto(
    string ClienteId, string Cliente, string ProdutoId, int LinhaProdutoId,
    int CentroId, string Centro, string Semana, double VolumeM3, bool Cif, long Prioridade,
    string MotivoSemana, string MotivoPlanta, double FolgaAntesM3,
    int PlantasElegiveis, int PosicaoPrioridade);

public sealed record SaldoDto(
    string ClienteId, string ProdutoId, int LinhaProdutoId,
    double VolumeM3, double DemandaM3, long Prioridade, string Motivo,
    double MaiorFolgaM3, double PisoM3);

public sealed record ExecucaoDto(
    DateTime GeradoEm, Config Config, IReadOnlyList<string> Notas,
    IReadOnlyList<string> Horizonte, SolverDto Solver, ResumoDto Resumo,
    IReadOnlyList<OcupacaoDto> Ocupacao, IReadOnlyList<AlocacaoDto> Alocacoes,
    IReadOnlyList<SaldoDto> NaoAlocado, IReadOnlyList<SaldoDto> ExcluidoPreflight,
    CarretasDto Carretas, IReadOnlyList<TermoDto> Criterios);

public sealed class Executor
{
    private readonly Dictionary<string, Carregador> _cache = new();
    private readonly SemaphoreSlim _trava = new(1, 1);

    public async Task<Carregador> CarregarAsync(string pasta)
    {
        var chave = Path.GetFullPath(pasta);

        await _trava.WaitAsync();
        try
        {
            if (_cache.TryGetValue(chave, out var existente)) return existente;
            var carregado = await Carregador.CarregarAsync(chave);
            _cache[chave] = carregado;
            return carregado;
        }
        finally { _trava.Release(); }
    }

    public ExecucaoDto Executar(Carregador dados, Config config)
    {
        var notas = new List<string>(dados.Notas);

        if (config.CarteiraSintetica is { } parametros)
            dados = dados.ComCarteira(GeradorCarteira.Gerar(dados, parametros, notas));

        var bruta = ProvedorCapacidade.MontarBruta(config, dados.Capacidade, dados.ParesElegiveis, notas);
        var prep = Preparador.Preparar(dados, bruta.Pares, config, notas);

        var demandaPorLinha = prep.Itens
            .GroupBy(i => i.LinhaProdutoId)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.VolumeM3));

        var capacidade = ProvedorCapacidade.Aplicar(config, bruta, demandaPorLinha, notas);
        var resultado = Otimizacao.Resolver(config, prep.Itens, capacidade, notas);

        return Montar(config, dados, prep, capacidade, resultado, notas);
    }

    private static CarretasDto MontarCarretas(
        Config config, ResultadoOtimizacao resultado, CapacidadeHorizonte capacidade,
        Dictionary<int, string> nomeCentro)
    {
        var e = resultado.Embarques;
        var totalCarretas = e.Sum(x => x.Carretas);
        var volumeTotal = e.Sum(x => x.VolumeM3);

        var porClienteSemana = e
            .GroupBy(x => (x.ClienteId, x.IndiceSemana))
            .Select(g => (g.Key.ClienteId, Carretas: g.Sum(x => x.Carretas)))
            .Where(t => t.Carretas > 0)
            .ToList();

        var limites = config.LimiteRecebimento;
        var noLimite = limites.Ativo
            ? porClienteSemana.Where(t => t.Carretas >= limites.De(t.ClienteId)).ToList()
            : [];

        return new CarretasDto(
            config.Carreta.Ativa,
            totalCarretas,
            e.Count,
            totalCarretas > 0 ? Math.Round(volumeTotal / totalCarretas, 2) : 0,
            totalCarretas > 0 ? Math.Round(volumeTotal / (totalCarretas * config.Carreta.MaximoM3), 4) : 0,
            config.Carreta.MinimoM3,
            config.Carreta.MaximoM3,
            e.Take(50).Select(x => new EmbarqueDto(
                x.ClienteId, x.CentroId, nomeCentro.GetValueOrDefault(x.CentroId, x.CentroId.ToString()),
                capacidade.Semanas[x.IndiceSemana].ToString(), x.Carretas, x.VolumeM3,
                x.Carretas > 0 ? Math.Round(x.VolumeM3 / (x.Carretas * config.Carreta.MaximoM3), 4) : 0))
            .ToList(),
            new RecebimentoDto(
                limites.Ativo,
                limites.CarretasPorSemana,
                limites.PorCliente.Count,
                noLimite.Select(t => t.ClienteId).Distinct().Count(),
                noLimite.Count,
                porClienteSemana.Count > 0 ? porClienteSemana.Max(t => t.Carretas) : 0));
    }

    private static ExecucaoDto Montar(
        Config config, Carregador dados, Preparacao prep,
        CapacidadeHorizonte capacidade, ResultadoOtimizacao resultado, List<string> notas)
    {
        var nomeCentro = dados.Centros.ToDictionary(c => c.CentroId, c => c.Nome);
        var itensPorIndice = prep.Itens.ToDictionary(i => i.Indice);

        var (explicaAloc, explicaSaldo) = Explicador.Explicar(
            prep.Itens, capacidade, resultado.Alocacoes,
            resultado.NaoAlocadoPorItem, resultado.PrioridadePorItem);

        var alocado = resultado.Alocacoes.Sum(a => a.VolumeM3);
        var naoAlocado = resultado.NaoAlocadoPorItem.Values.Sum();

        var porCentroSemana = resultado.Alocacoes
            .GroupBy(a => (a.CentroId, a.IndiceSemana))
            .ToDictionary(g => g.Key, g => g.Sum(a => a.VolumeM3));

        var capacidadePorCentroSemana = capacidade.PorBucket
            .GroupBy(kv => (kv.Key.Centro, kv.Key.IndiceSemana))
            .ToDictionary(g => g.Key, g => (double)g.Sum(kv => kv.Value));

        var centros = capacidadePorCentroSemana.Keys.Select(k => k.Centro).Distinct().OrderBy(c => c).ToList();

        var ocupacao = centros
            .SelectMany(centro => capacidade.Semanas.Select((sem, s) => new OcupacaoDto(
                centro,
                nomeCentro.GetValueOrDefault(centro, centro.ToString()),
                sem.ToString(),
                s,
                Math.Round(porCentroSemana.GetValueOrDefault((centro, s), 0), 2),
                Math.Round(capacidadePorCentroSemana.GetValueOrDefault((centro, s), 0), 2))))
            .ToList();

        return new ExecucaoDto(
            GeradoEm: DateTime.UtcNow,
            Config: config,
            Notas: notas,
            Horizonte: capacidade.Semanas.Select(s => s.ToString()).ToList(),
            Solver: new SolverDto(
                resultado.Status, Math.Round(resultado.Segundos, 3), resultado.Objetivo,
                resultado.Variaveis, resultado.Binarias, Math.Round(resultado.GreedyM3, 2)),
            Resumo: new ResumoDto(
                Math.Round(prep.DemandaTotalM3, 2),
                Math.Round(prep.DemandaElegivelM3, 2),
                Math.Round(prep.DemandaExcluidaM3, 2),
                Math.Round(alocado, 2),
                Math.Round(naoAlocado, 2),
                capacidade.Total,
                Math.Round(capacidade.FatorAplicado, 4),
                prep.DemandaElegivelM3 > 0 ? Math.Round(alocado / prep.DemandaElegivelM3, 4) : 0,
                prep.Itens.Count,
                prep.Excluidos.Count),
            Ocupacao: ocupacao,
            Alocacoes: resultado.Alocacoes.Select(a =>
            {
                var e = explicaAloc[(a.ItemIndice, a.CentroId, a.IndiceSemana)];
                return new AlocacaoDto(
                    itensPorIndice[a.ItemIndice].ClienteId,
                    itensPorIndice[a.ItemIndice].ClienteNome,
                    itensPorIndice[a.ItemIndice].ProdutoId,
                    itensPorIndice[a.ItemIndice].LinhaProdutoId,
                    a.CentroId,
                    nomeCentro.GetValueOrDefault(a.CentroId, a.CentroId.ToString()),
                    capacidade.Semanas[a.IndiceSemana].ToString(),
                    Math.Round(a.VolumeM3, 2),
                    itensPorIndice[a.ItemIndice].Cif,
                    resultado.PrioridadePorItem[a.ItemIndice],
                    e.MotivoSemana, e.MotivoPlanta, e.FolgaAntesM3,
                    e.PlantasElegiveis, e.PosicaoPrioridade);
            }).ToList(),
            NaoAlocado: resultado.NaoAlocadoPorItem
                .Select(kv => new SaldoDto(
                    itensPorIndice[kv.Key].ClienteId,
                    itensPorIndice[kv.Key].ProdutoId,
                    itensPorIndice[kv.Key].LinhaProdutoId,
                    Math.Round(kv.Value, 2),
                    Math.Round(itensPorIndice[kv.Key].VolumeM3, 2),
                    resultado.PrioridadePorItem[kv.Key],
                    explicaSaldo[kv.Key].Motivo,
                    explicaSaldo[kv.Key].MaiorFolgaM3,
                    explicaSaldo[kv.Key].PisoM3))
                .OrderByDescending(x => x.VolumeM3).ToList(),
            Carretas: MontarCarretas(config, resultado, capacidade, nomeCentro),
            Criterios: resultado.Termos
                .Select(x => new TermoDto(x.Nome, x.Descricao, x.Ordem, x.Peso, Math.Round(x.Valor, 2)))
                .ToList(),
            ExcluidoPreflight: prep.Excluidos
                .Select(e => new SaldoDto(
                    e.ClienteId, e.ProdutoId, e.LinhaProdutoId,
                    Math.Round(e.VolumeM3, 2), Math.Round(e.VolumeM3, 2), 0,
                    e.Motivo.ToString(), 0, 0))
                .OrderByDescending(x => x.VolumeM3).ToList());
    }
}
