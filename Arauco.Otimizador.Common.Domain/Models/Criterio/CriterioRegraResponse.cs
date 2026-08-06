using Arauco.Otimizador.Common.Domain.Enums.Criterio;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Arauco.Otimizador.Common.Domain.Models.Criterio;

// Item da lista `criterios` retornada em CenarioDetalheResponse. Mesma forma de CriterioRegraRequest
// — não inclui o nome legível do critério (o front resolve via criterioChave na lista fixa).
// `criterioChave` é tipado como CriterioChaveEnum (int) e transmitido como inteiro (changelog 2026-08-03).
public class CriterioRegraResponse
{
    public CriterioChaveEnum CriterioChave { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    public OperadorCriterioEnum Operador { get; set; }

    public string Valor { get; set; }

    public int Peso { get; set; }
}