namespace Arauco.Otimizador.Common.Domain.Models.OtimizadorV2;

public class MoverPedidoV2Request
{
    public string PedidoId { get; set; }
    public int AnoDestino { get; set; }
    public int SemanaDestino { get; set; }
}
