namespace Arauco.Otimizador.Common.Domain.Models.Pedido;

// Enviado em PATCH /cenarios/{id}/pedidos/mover (spec §3.14).
public class MoverPedidoRequest
{
    public string PedidoId { get; set; }
    public int AnoDestino { get; set; }
    public int SemanaDestino { get; set; }
}