using Arauco.Otimizador.Common.Domain.Enums.Cenario;

namespace Arauco.Otimizador.Data.Entities.Cenario;

public class Cenario
{
    public string CenarioId { get; set; }
    public string Nome { get; set; }
    public string? ArquivoNome { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime? DataUltimoProcessamento { get; set; }
    public StatusCenarioEnum StatusEnum { get; set; }
    public bool Submetido { get; set; }
}
