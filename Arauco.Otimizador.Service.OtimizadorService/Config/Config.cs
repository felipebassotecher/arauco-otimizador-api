namespace Arauco.Otimizador.Service.OtimizadorService;

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

    public int ChapasPorLote { get; set; } = 40;

    public bool LoteMinimoEmChapas { get; set; } = true;

    public Carreta Carreta { get; set; } = new();

    public LimiteRecebimento LimiteRecebimento { get; set; } = new();

    public Criterios Criterios { get; set; } = new();

    public MixFrete MixFrete { get; set; } = new();

    public Pesos Pesos { get; set; } = new();

    public double LimiteSegundos { get; set; } = 60;

    public bool LogSolver { get; set; }

    public int Threads { get; set; }
}

public sealed class Carreta
{
    public bool Ativa { get; set; } = true;
    public double MinimoM3 { get; set; } = 25;
    public double MaximoM3 { get; set; } = 30;
    public double? MinimoSkuM3 { get; set; }
    public int MaximoCarretasPorEmbarque { get; set; } = 400;
}

public sealed class LimiteRecebimento
{
    public bool Ativo { get; set; }
    public int CarretasPorSemana { get; set; } = 3;
    public Dictionary<string, int> PorCliente { get; set; } = [];

    public int De(string clienteId) =>
        PorCliente.TryGetValue(clienteId, out var v) ? v : CarretasPorSemana;
}

public sealed class Criterios
{
    public ModoObjetivo Modo { get; set; } = ModoObjetivo.Ranking;
    public int Atender { get; set; } = 1;
    public int Antiguidade { get; set; } = 2;
    public int MixFrete { get; set; } = 3;
    public int Atraso { get; set; } = 4;
    public int Industria { get; set; } = 5;
    public int Suavizacao { get; set; } = 6;
    public double ToleranciaLexicografica { get; set; } = 0.02;
}

public enum ModoObjetivo
{
    Ranking,
    Lexicografico
}

public sealed class MixFrete
{
    public bool Ativo { get; set; } = true;
    public double AlvoCif { get; set; } = 0.6;
}

public sealed class Pesos
{
    public double Antiguidade { get; set; } = 50;
    public double Cif { get; set; } = 0;
    public double Industria { get; set; } = 20;
}
