namespace Arauco.Otimizador.Common.Domain.Models.Otimizador;

public class OtimizacaoNaoAlocadoResponse
{
    public string Cliente { get; set; }
    public string Produto { get; set; }
    public int LinhaProdutoId { get; set; }
    public double VolumeM3 { get; set; }
    public double DemandaM3 { get; set; }
    public long Prioridade { get; set; }
    public string Motivo { get; set; }
    public double MaiorFolgaM3 { get; set; }
    public double PisoM3 { get; set; }
}
