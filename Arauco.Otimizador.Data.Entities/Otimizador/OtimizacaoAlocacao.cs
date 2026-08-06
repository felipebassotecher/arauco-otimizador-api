namespace Arauco.Otimizador.Data.Entities.Otimizador;

public class OtimizacaoAlocacao
{
    public string AlocacaoId { get; set; }
    public string ResultadoId { get; set; }
    public string Cliente { get; set; }
    public string Produto { get; set; }
    public int LinhaProdutoId { get; set; }
    public int CentroId { get; set; }
    public string Centro { get; set; }
    public int Ano { get; set; }
    public int Semana { get; set; }
    public decimal VolumeM3 { get; set; }
    public bool Cif { get; set; }
    public long Prioridade { get; set; }
    public string MotivoSemana { get; set; }
    public string MotivoPlanta { get; set; }
    public decimal FolgaAntesM3 { get; set; }
    public int PlantasElegiveis { get; set; }
    public int PosicaoPrioridade { get; set; }
}
