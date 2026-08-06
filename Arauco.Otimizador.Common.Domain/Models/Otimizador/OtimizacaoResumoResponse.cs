namespace Arauco.Otimizador.Common.Domain.Models.Otimizador;

public class OtimizacaoResumoResponse
{
    public double DemandaTotalM3 { get; set; }
    public double DemandaElegivelM3 { get; set; }
    public double ExcluidoPreflightM3 { get; set; }
    public double AlocadoM3 { get; set; }
    public double NaoAlocadoM3 { get; set; }
    public long CapacidadeTotal { get; set; }
    public double FatorCapacidade { get; set; }
    public double PercentualAlocado { get; set; }
    public int Itens { get; set; }
    public int ItensExcluidos { get; set; }
}
