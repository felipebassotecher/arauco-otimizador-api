using Arauco.Otimizador.Common.Domain.Enums.Cenario;
using Arauco.Otimizador.Common.Domain.Models.Parametro;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Arauco.Otimizador.Common.Domain.Models.Cenario;

// Retornado por POST /Cenarios/{id}/processar — estado completo do cenário após gerar os pedidos.
public class CenarioProcessamentoResponse
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
