using Arauco.Otimizador.Common.Domain.Models.Criterio;

namespace Arauco.Otimizador.Common.Domain.Models.Cenario;

// Enviado em PUT /cenarios/{id} (spec §3.4.2).
public class CenarioAtualizacaoRequest
{
    public string Nome { get; set; }
    public List<CriterioRegraRequest> Criterios { get; set; }
}