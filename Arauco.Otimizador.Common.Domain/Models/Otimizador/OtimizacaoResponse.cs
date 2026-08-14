namespace Arauco.Otimizador.Common.Domain.Models.Otimizador;

public class OtimizacaoResponse
{
    public string ResultadoId { get; set; }
    public DateTime GeradoEm { get; set; }
    public IReadOnlyList<string> Horizonte { get; set; }
    public OtimizacaoSolverResponse Solver { get; set; }
    public OtimizacaoResumoResponse Resumo { get; set; }
    public List<OtimizacaoAlocacaoResponse> Alocacoes { get; set; }
    public List<OtimizacaoNaoAlocadoResponse> NaoAlocado { get; set; }
    public List<string> Notas { get; set; }
}
