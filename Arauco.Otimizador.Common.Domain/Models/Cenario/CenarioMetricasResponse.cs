namespace Arauco.Otimizador.Common.Domain.Models.Cenario;

public class CenarioMetricasResponse
{
    public int QuantidadeDemandas { get; set; }
    public int QuantidadePedidos { get; set; }
    public decimal VolumeTotal { get; set; }
    public List<CenarioMetricaSemanaResponse> VolumePorSemana { get; set; }
    public List<CenarioOcupacaoPlantaResponse> OcupacaoPlanta { get; set; }
}
