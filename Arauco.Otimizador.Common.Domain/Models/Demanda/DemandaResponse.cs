namespace Arauco.Otimizador.Common.Domain.Models.Demanda;

// Retornado por GET /demandas?cenarioId={id} e POST /demandas/upload. Não inclui cenarioId (o cliente
// já o informa via query param/payload). `tipoFrete` é texto livre (CIF/FOB por convenção), não enum
// fechado (spec §3.11).
public class DemandaResponse
{
    public string Id { get; set; }
    public string Cliente { get; set; }
    public string Material { get; set; }
    public decimal Volume { get; set; }
    public DateTime DataEntregaDesejada { get; set; }
    public string TipoFrete { get; set; }
    public string Segmento { get; set; }
}