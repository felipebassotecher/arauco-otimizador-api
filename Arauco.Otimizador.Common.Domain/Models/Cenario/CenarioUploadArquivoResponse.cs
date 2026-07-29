using Arauco.Otimizador.Common.Domain.Enums.Cenario;
using Arauco.Otimizador.Common.Domain.Models.Parametro;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Arauco.Otimizador.Common.Domain.Models.Cenario;

// Retornado por POST /Cenarios/{id}/csv — estado completo do cenário após a substituição das
// demandas (o front tipicamente re-renderiza a tela de detalhe após o upload).
public class CenarioUploadArquivoResponse
{
    public string Id { get; set; }
    public string Nome { get; set; }
    public List<ParametroListaResponse> Parametros { get; set; }
    public string? ArquivoNome { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime? DataUltimoProcessamento { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    public StatusCenarioEnum Status { get; set; }
    public bool Submetido { get; set; }
    public SemanaAnoResponse? PrimeiraSemana { get; set; }
    public SemanaAnoResponse? UltimaSemana { get; set; }
}
