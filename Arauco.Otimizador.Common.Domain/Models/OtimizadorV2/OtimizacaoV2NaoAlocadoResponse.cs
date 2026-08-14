namespace Arauco.Otimizador.Common.Domain.Models.OtimizadorV2;

public class OtimizacaoV2NaoAlocadoResponse
{
    public string Cliente { get; set; }
    public string Material { get; set; }
    public int LinhaProdutoId { get; set; }
    public double VolumeM3 { get; set; }
    public string Motivo { get; set; }
}
