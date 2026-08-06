using Arauco.Otimizador.Common.Domain.Enums.Criterio;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Arauco.Otimizador.Common.Domain.Models.Criterio;

// Item da lista `criterios` enviada em CenarioCriacaoRequest/CenarioAtualizacaoRequest.
// Representa uma regra: um critério (criterioChave) comparado via `operador` a `valor`, com um
// `peso` de -100 a 100 (negativo penaliza, positivo prioriza). O mesmo criterioChave pode se
// repetir em mais de uma regra da mesma lista. `valor` é sempre string; a API interpreta como
// texto ou número conforme o TipoCriterioEnum do critério referenciado por criterioChave.
//
// `criterioChave` é um enum fechado (CriterioChaveEnum, int) — transmitido como o valor inteiro
// (ex.: 1 para TipoFrete). Valores fora do enum são rejeitados com 400 Bad Request (changelog 2026-08-03).
public class CriterioRegraRequest
{
    public CriterioChaveEnum CriterioChave { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    public OperadorCriterioEnum Operador { get; set; }

    public string Valor { get; set; }

    public int Peso { get; set; }
}