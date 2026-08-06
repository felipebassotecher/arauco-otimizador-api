using Arauco.Otimizador.Common.Domain.Enums.Criterio;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Arauco.Otimizador.Common.Domain.Models.Criterio;

// Retornado por GET /cenarios/criterios-disponiveis (changelog 2026-08-03 / spec §3.10.1). Lista fixa,
// definida em código na API (CriteriosDisponiveis) — não há CRUD/tabela para isso.
public class CriterioDisponivelResponse
{
    public CriterioChaveEnum Chave { get; set; }

    public string Nome { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    public TipoCriterioEnum Tipo { get; set; }
}