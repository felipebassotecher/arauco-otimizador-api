namespace Arauco.Otimizador.Common.Domain.Models.Otimizador;

public class PedidoOtimizadoNaoAlocadoResponse
{
    public string Id { get; set; }
    public string Cliente { get; set; }
    public string Material { get; set; }
    public int LinhaProdutoId { get; set; }
    public double VolumeM3 { get; set; }
    public string Motivo { get; set; }
    public List<PedidoOtimizadoMotivoResponse> Motivos { get; set; }
}
