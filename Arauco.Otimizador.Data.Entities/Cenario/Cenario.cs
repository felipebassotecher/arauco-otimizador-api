using Arauco.Otimizador.Common.Domain.Enums.Cenario;

namespace Arauco.Otimizador.Data.Entities.Cenario;

public class Cenario
{
    public string CenarioId { get; set; }
    public string Nome { get; set; }
    // Setup do qual o motor de otimização lê horizonte, capacidade, carreta, limite de recebimento e
    // ordem de importância dos critérios — escolhido na criação do cenário, imutável depois (ver
    // CenarioService.CriarAsync). Nulo apenas para cenários criados antes deste vínculo existir.
    public string? SetupId { get; set; }
    public string? ArquivoNome { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime? DataUltimoProcessamento { get; set; }
    public StatusCenarioEnum StatusEnum { get; set; }
    public bool Submetido { get; set; }
}
