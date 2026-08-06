namespace Arauco.Otimizador.Common.Domain.Models.Pedido;

// Retornado por GET /cenarios/{id}/semanas/{ano}/{semana}/pedidos e PATCH /cenarios/{id}/pedidos/mover.
// Não inclui cenarioId (já dado pela URL) nem grupo (critério de agrupamento interno, sem uso no front).
// `tipoFrete` é texto livre (CIF/FOB por convenção), não enum fechado (spec §3.13).
public class PedidoResponse
{
    public string Id { get; set; }
    public string Cliente { get; set; }
    public string TipoFrete { get; set; }
    public decimal Volume { get; set; }
    public DateTime DataEntregaPrevista { get; set; }
    public int Ano { get; set; }
    public int Semana { get; set; }
    public bool Pinado { get; set; }
}