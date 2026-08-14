namespace Arauco.Otimizador.Common.Domain.Models.OtimizadorV2;

public class OtimizacaoV2Response
{
    public string ResultadoId { get; set; }
    public DateTime GeradoEm { get; set; }
    public IReadOnlyList<string> Horizonte { get; set; }
    public OtimizacaoV2SolverResponse Solver { get; set; }
    public OtimizacaoV2ResumoResponse Resumo { get; set; }
    public List<OtimizacaoV2AlocacaoResponse> Alocacoes { get; set; }
    public List<OtimizacaoV2NaoAlocadoResponse> NaoAlocado { get; set; }
    public List<string> Notas { get; set; }
}
