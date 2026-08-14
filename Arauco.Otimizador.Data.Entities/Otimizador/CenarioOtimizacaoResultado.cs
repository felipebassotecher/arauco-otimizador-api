namespace Arauco.Otimizador.Data.Entities.Otimizador;

public class CenarioOtimizacaoResultado
{
    public string ResultadoId { get; set; }
    public string CenarioId { get; set; }
    public DateTime GeradoEm { get; set; }
    public string StatusSolver { get; set; }
    public double Segundos { get; set; }
    public double Objetivo { get; set; }
    public int Variaveis { get; set; }
    public int Binarias { get; set; }
    public long CapacidadeTotal { get; set; }
    public decimal DemandaTotalM3 { get; set; }
    public decimal DemandaElegivelM3 { get; set; }
    public decimal AlocadoM3 { get; set; }
    public decimal NaoAlocadoM3 { get; set; }
    public int Itens { get; set; }
    public int ItensExcluidos { get; set; }
}
