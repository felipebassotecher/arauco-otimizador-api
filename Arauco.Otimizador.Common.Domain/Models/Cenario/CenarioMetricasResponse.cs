namespace Arauco.Otimizador.Common.Domain.Models.Cenario;

public class CenarioMetricasResponse
{
    public int QuantidadeDemandas { get; set; }
    public int QuantidadePedidos { get; set; }
    public int PedidosAlocados { get; set; }
    public int PedidosNaoAlocados { get; set; }
    public decimal VolumeTotal { get; set; }
    public decimal VolumeTotalAlocado { get; set; }
    public decimal VolumeTotalNaoAlocado { get; set; }
    public List<CenarioMetricaSemanaResponse> VolumePorSemana { get; set; }
    public List<CenarioOcupacaoPlantaResponse> OcupacaoPlanta { get; set; }
}
