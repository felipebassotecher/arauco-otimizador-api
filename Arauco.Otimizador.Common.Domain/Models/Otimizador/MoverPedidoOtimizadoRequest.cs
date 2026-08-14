namespace Arauco.Otimizador.Common.Domain.Models.Otimizador;

public class MoverPedidoOtimizadoRequest
{
    public string PedidoId { get; set; }
    public int AnoDestino { get; set; }
    public int SemanaDestino { get; set; }
}
