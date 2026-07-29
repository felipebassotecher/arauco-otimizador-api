using Arauco.Otimizador.Common.Domain.Enums.Demanda;

namespace Arauco.Otimizador.Data.Entities.Pedido;

public class Pedido
{
    public string PedidoId { get; set; }
    public string CenarioId { get; set; }
    public string Cliente { get; set; }
    public TipoFreteEnum TipoFreteEnum { get; set; }
    public decimal Volume { get; set; }
    public DateTime DataEntregaPrevista { get; set; }
    public int Ano { get; set; }
    public int Semana { get; set; }
    public bool Pinado { get; set; }
    public string? Grupo { get; set; }
}
