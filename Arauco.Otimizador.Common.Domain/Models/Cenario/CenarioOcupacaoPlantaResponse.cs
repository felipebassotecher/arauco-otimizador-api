namespace Arauco.Otimizador.Common.Domain.Models.Cenario;

// Ocupação de cada planta (Centro) em uma semana do horizonte otimizado. O percentual é calculado
// sobre a capacidade real declarada (`Capacidade`, TipoAlocacao = MercadoInterno) somada por planta —
// não é a capacidade calibrada/simulada que o motor de otimização efetivamente usou para decidir a
// alocação (essa é específica de cada execução e não fica persistida), então este número é uma leitura
// "capacidade declarada x volume alocado", não uma reconstrução exata do que o solver viu.
public class CenarioOcupacaoPlantaResponse
{
    public int Ano { get; set; }
    public int Semana { get; set; }
    public DateTime Data { get; set; }
    public List<CenarioOcupacaoCentroResponse> Plantas { get; set; }
}

public class CenarioOcupacaoCentroResponse
{
    public int CentroId { get; set; }
    public string Centro { get; set; }
    public double Percentual { get; set; }
}
