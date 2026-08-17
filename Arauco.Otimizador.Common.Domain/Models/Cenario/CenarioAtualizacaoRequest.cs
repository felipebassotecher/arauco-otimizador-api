namespace Arauco.Otimizador.Common.Domain.Models.Cenario;

// Enviado em PUT /cenarios/{id} (spec §3.4.2). O setup vinculado é fixado na criação do cenário e não
// é editável aqui — ver CenarioCriacaoRequest.SetupId.
public class CenarioAtualizacaoRequest
{
    public string Nome { get; set; }
}
