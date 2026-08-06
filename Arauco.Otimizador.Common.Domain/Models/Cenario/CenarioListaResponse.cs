namespace Arauco.Otimizador.Common.Domain.Models.Cenario;

// Retornado por GET /cenarios. Forma resumida usada na listagem — não inclui status, arquivoNome,
// criterios nem primeiraSemana/ultimaSemana (spec §3.2).
public class CenarioListaResponse
{
    public string Id { get; set; }
    public string Nome { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime? DataUltimoProcessamento { get; set; }
    public bool Submetido { get; set; }
}