namespace Arauco.Otimizador.Data.Entities.Otimizador;

public class OtimizacaoOcupacao
{
    public string OcupacaoId { get; set; }
    public string ResultadoId { get; set; }
    public int CentroId { get; set; }
    public string Centro { get; set; }
    public int Ano { get; set; }
    public int Semana { get; set; }
    public decimal AlocadoM3 { get; set; }
    public decimal CapacidadeM3 { get; set; }
}
