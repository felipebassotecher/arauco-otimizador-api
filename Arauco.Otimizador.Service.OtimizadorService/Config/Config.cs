namespace Arauco.Otimizador.Service.OtimizadorService;

// Configuração do motor de otimização. Os campos que também existem no Setup (Horizonte, Capacidade,
// SemanaInicial, AlvoCapacidadeSobreDemanda, Carreta.Ativa/MinimoM3/MaximoM3,
// LimiteRecebimento.Ativo/CarretasPorSemana, MixFrete.Ativo/AlvoCif, QuantidadeMinimaSkuPorLote) são
// sobrescritos a partir do Setup vinculado ao cenário — ver OtimizadorService.CriarConfig. Os valores
// abaixo só valem como fallback (Setup sem o campo preenchido) ou para campos sem equivalente no
// Setup, que continuam hard-coded aqui por decisão de produto.
public sealed class Config
{
    public int Horizonte { get; set; } = 8;

    public ModoCapacidade Capacidade { get; set; } = ModoCapacidade.Simulada;

    public string? SemanaInicial { get; set; }

    public double? AlvoCapacidadeSobreDemanda { get; set; } = 0.8;

    public double FatorCapacidade { get; set; } = 1.0;

    public bool CalibrarPorLinhaProduto { get; set; } = true;

    public double FracaoEspalhamento { get; set; } = 0.7;

    public Dictionary<int, double> FatorPorCentro { get; set; } = [];

    public Dictionary<int, double> FatorPorLinhaProduto { get; set; } = [];

    // Fallback para o piso por SKU (Modelo/Preparacao.cs) quando o setup não informa
    // QuantidadeMinimaSkuPorLote.
    public int ChapasPorLote { get; set; } = 40;

    public bool LoteMinimoEmChapas { get; set; } = true;

    // Quantidade mínima de chapas por SKU dentro de um embarque (piso de fragmentação de um item
    // divisível) — vem de Setup.QuantidadeMinimaSkuPorLote; nulo usa ChapasPorLote.
    public int? QuantidadeMinimaSkuPorLote { get; set; }

    public Carreta Carreta { get; set; } = new();

    public LimiteRecebimento LimiteRecebimento { get; set; } = new();

    public MixFrete MixFrete { get; set; } = new();

    public double LimiteSegundos { get; set; } = 60;

    public bool LogSolver { get; set; }

    public int Threads { get; set; }
}

public sealed class Carreta
{
    // Ativa somente quando o setup informa volume mínimo E máximo de carreta — ver CriarConfig.
    public bool Ativa { get; set; }
    public double MinimoM3 { get; set; } = 25;
    public double MaximoM3 { get; set; } = 30;
    public int MaximoCarretasPorEmbarque { get; set; } = 400;
}

public sealed class LimiteRecebimento
{
    // Ativo somente quando o setup informa CapacidadeMaximaRecebimentoCliente — ver CriarConfig.
    public bool Ativo { get; set; }
    public int CarretasPorSemana { get; set; } = 3;
    public Dictionary<string, int> PorCliente { get; set; } = [];

    public int De(string clienteId) =>
        PorCliente.TryGetValue(clienteId, out var v) ? v : CarretasPorSemana;
}

public sealed class MixFrete
{
    // Ativo somente quando o setup informa MixTipoFrete — ver CriarConfig.
    public bool Ativo { get; set; }
    public double AlvoCif { get; set; } = 0.6;
}
