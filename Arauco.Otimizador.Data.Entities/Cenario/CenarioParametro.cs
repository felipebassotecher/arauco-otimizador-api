namespace Arauco.Otimizador.Data.Entities.Cenario;

// Vínculo N:N entre Cenario e Parametro (parâmetros selecionados na criação do cenário).
public class CenarioParametro
{
    public string CenarioId { get; set; }
    public string ParametroId { get; set; }
}
