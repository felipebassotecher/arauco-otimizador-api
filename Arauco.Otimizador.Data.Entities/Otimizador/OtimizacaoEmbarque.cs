namespace Arauco.Otimizador.Data.Entities.Otimizador;

public class OtimizacaoEmbarque
{
    public string EmbarqueId { get; set; }
    public string ResultadoId { get; set; }
    public string Cliente { get; set; }
    public int CentroId { get; set; }
    public string Centro { get; set; }
    public int Ano { get; set; }
    public int Semana { get; set; }
    public int Carretas { get; set; }
    public decimal VolumeM3 { get; set; }
    public decimal OcupacaoMedia { get; set; }
}
