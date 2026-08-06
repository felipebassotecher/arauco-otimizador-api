namespace Arauco.Otimizador.Common.Domain.Models.Otimizador;

public class OtimizacaoAlocacaoResponse
{
    public string Cliente { get; set; }
    public string Produto { get; set; }
    public int LinhaProdutoId { get; set; }
    public int CentroId { get; set; }
    public string Centro { get; set; }
    public string Semana { get; set; }
    public double VolumeM3 { get; set; }
    public bool Cif { get; set; }
    public long Prioridade { get; set; }
    public string MotivoSemana { get; set; }
    public string MotivoPlanta { get; set; }
    public double FolgaAntesM3 { get; set; }
    public int PlantasElegiveis { get; set; }
    public int PosicaoPrioridade { get; set; }
}
