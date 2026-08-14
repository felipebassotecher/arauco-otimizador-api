namespace Arauco.Otimizador.Common.Domain.Models.Cenario;

/// <summary>
/// Retornado por POST /cenarios/{id}/processar. Contém apenas o resultado da
/// execução (sucesso/erro e tempo decorrido), sem os dados do cenário — o
/// detalhe atualizado deve ser consultado em GET /cenarios/{id} quando necessário.
/// </summary>
public class ProcessarCenarioResponse
{
    public bool Sucesso { get; set; }
    public double TempoSegundos { get; set; }
}
