using Arauco.Otimizador.Common.Domain.Enums.Demanda;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Arauco.Otimizador.Common.Domain.Models.Pedido;

// Retornado por PATCH /Cenarios/{id}/pedidos/mover — o pedido já com a nova semana/pinado.
public class PedidoMovimentacaoResponse
{
    public string Id { get; set; }
    public string CenarioId { get; set; }
    public string Cliente { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    public TipoFreteEnum TipoFrete { get; set; }
    public decimal Volume { get; set; }
    public DateTime DataEntregaPrevista { get; set; }
    public int Ano { get; set; }
    public int Semana { get; set; }
    public bool Pinado { get; set; }
    public string? Grupo { get; set; }
}
