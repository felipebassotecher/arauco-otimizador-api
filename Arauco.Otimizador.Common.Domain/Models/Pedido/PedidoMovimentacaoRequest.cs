namespace Arauco.Otimizador.Common.Domain.Models.Pedido;

// Enviado em PATCH /Cenarios/{id}/pedidos/mover.
public class PedidoMovimentacaoRequest
{
    public string PedidoId { get; set; }
    public int AnoDestino { get; set; }
    public int SemanaDestino { get; set; }
}
