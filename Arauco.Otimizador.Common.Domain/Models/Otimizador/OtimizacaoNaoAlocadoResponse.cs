namespace Arauco.Otimizador.Common.Domain.Models.Otimizador;

public class OtimizacaoNaoAlocadoResponse
{
    public string Cliente { get; set; }
    public string Material { get; set; }
    public int LinhaProdutoId { get; set; }
    public double VolumeM3 { get; set; }
    public string Motivo { get; set; }
}
