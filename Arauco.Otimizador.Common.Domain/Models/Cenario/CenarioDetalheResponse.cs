using Arauco.Otimizador.Common.Domain.Enums.Cenario;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Arauco.Otimizador.Common.Domain.Models.Cenario;

// Retornado por GET /cenarios/{id} e, por representarem o mesmo recurso já atualizado, também por
// PUT /cenarios/{id}, POST /cenarios/{id}/csv, POST /cenarios/{id}/processar e POST /cenarios/{id}/submeter (spec §3.3).
public class CenarioDetalheResponse
{
    public string Id { get; set; }
    public string Nome { get; set; }
    public string? SetupId { get; set; }
    public string? SetupNome { get; set; }
    public string? ArquivoNome { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime? DataUltimoProcessamento { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    public StatusCenarioEnum Status { get; set; }
    public bool Submetido { get; set; }
    public SemanaAnoResponse? PrimeiraSemana { get; set; }
    public SemanaAnoResponse? UltimaSemana { get; set; }
}
