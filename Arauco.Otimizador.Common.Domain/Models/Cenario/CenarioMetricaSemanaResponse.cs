namespace Arauco.Otimizador.Common.Domain.Models.Cenario;

public class CenarioMetricaSemanaResponse
{
    public int Ano { get; set; }
    public int Semana { get; set; }
    public decimal Volume { get; set; }
    public int QuantidadePedidos { get; set; }
}
