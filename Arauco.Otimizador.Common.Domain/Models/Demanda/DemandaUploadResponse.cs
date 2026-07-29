using Arauco.Otimizador.Common.Domain.Enums.Demanda;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Arauco.Otimizador.Common.Domain.Models.Demanda;

// Retornado por POST /Demandas/upload — as demandas recém-importadas.
public class DemandaUploadResponse
{
    public string Id { get; set; }
    public string CenarioId { get; set; }
    public string Cliente { get; set; }
    public string Material { get; set; }
    public decimal Volume { get; set; }
    public DateTime DataEntregaDesejada { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    public TipoFreteEnum TipoFrete { get; set; }
}
