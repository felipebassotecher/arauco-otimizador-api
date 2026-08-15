using Arauco.Otimizador.Common.Domain.Enums.Setup;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Arauco.Otimizador.Common.Domain.Models.Setup;

public class SetupOrdemImportanciaRequest
{
    [JsonConverter(typeof(StringEnumConverter))]
    public CriterioOrdemEnum Criterio { get; set; }

    public int Ordem { get; set; }
    public string? Descricao { get; set; }
    public bool Ativo { get; set; }
}
