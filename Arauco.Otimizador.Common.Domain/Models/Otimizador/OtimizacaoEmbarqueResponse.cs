namespace Arauco.Otimizador.Common.Domain.Models.Otimizador;

public class OtimizacaoEmbarqueResponse
{
    public string Cliente { get; set; }
    public int CentroId { get; set; }
    public string Centro { get; set; }
    public string Semana { get; set; }
    public int Carretas { get; set; }
    public double VolumeM3 { get; set; }
    public double OcupacaoMedia { get; set; }
}
