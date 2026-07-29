using Arauco.Otimizador.Common.Domain.Enums.Cenario;
using Arauco.Otimizador.Common.Domain.Models.Parametro;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Arauco.Otimizador.Common.Domain.Models.Cenario;

// Retornado por POST /Cenarios. Não inclui ArquivoNome/DataUltimoProcessamento/Submetido/
// PrimeiraSemana/UltimaSemana — logo após a criação esses campos são sempre nulos/vazios/false,
// então não fazem parte do que é "necessário saber" nesse momento (ver GET /Cenarios/{id} para o
// estado completo).
public class CenarioCriacaoResponse
{
    public string Id { get; set; }
    public string Nome { get; set; }
    public List<ParametroListaResponse> Parametros { get; set; }
    public DateTime DataCriacao { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    public StatusCenarioEnum Status { get; set; }
}
