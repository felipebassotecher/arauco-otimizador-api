namespace Arauco.Otimizador.Data.Entities.OtimizadorV2;

public class PedidoV2NaoAlocado
{
    public string NaoAlocadoId { get; set; }
    public string ResultadoId { get; set; }
    public string Cliente { get; set; }
    public string Material { get; set; }
    public int LinhaProdutoId { get; set; }
    public decimal VolumeM3 { get; set; }
    public string Motivo { get; set; }
}
