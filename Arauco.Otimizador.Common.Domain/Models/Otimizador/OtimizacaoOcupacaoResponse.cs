namespace Arauco.Otimizador.Common.Domain.Models.Otimizador;

public class OtimizacaoOcupacaoResponse
{
    public int CentroId { get; set; }
    public string Centro { get; set; }
    public string Semana { get; set; }
    public double AlocadoM3 { get; set; }
    public double CapacidadeM3 { get; set; }
}
