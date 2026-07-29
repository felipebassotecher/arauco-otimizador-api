using Arauco.Otimizador.Common.Domain.Enums.Cenario;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Arauco.Otimizador.Common.Domain.Models.Cenario;

// Retornado por GET /Cenarios. Não inclui os Parametros do cenário (lista pesada, só relevante no
// detalhe) — ver CenarioDetalheResponse.
public class CenarioListaResponse
{
    public string Id { get; set; }
    public string Nome { get; set; }
    public string? ArquivoNome { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime? DataUltimoProcessamento { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    public StatusCenarioEnum Status { get; set; }
    public bool Submetido { get; set; }
    public SemanaAnoResponse? PrimeiraSemana { get; set; }
    public SemanaAnoResponse? UltimaSemana { get; set; }
}
