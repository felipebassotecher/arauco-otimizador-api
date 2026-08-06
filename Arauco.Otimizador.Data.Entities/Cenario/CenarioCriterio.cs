using Arauco.Otimizador.Common.Domain.Enums.Criterio;

namespace Arauco.Otimizador.Data.Entities.Cenario;

// Regra de critério pertencente a um cenário (spec §1/§3.9/§5.9 — não é um cadastro global). Cada
// linha combina um criterioChave + operador + valor de comparação + peso. O mesmo criterioChave pode
// aparecer em mais de uma linha do mesmo cenário.
public class CenarioCriterio
{
    public int Id { get; set; }
    public string CenarioId { get; set; }
    public string CriterioChave { get; set; }
    public OperadorCriterioEnum Operador { get; set; }
    public string Valor { get; set; }
    public int Peso { get; set; }
}