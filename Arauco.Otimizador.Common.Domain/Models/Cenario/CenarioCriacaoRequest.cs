namespace Arauco.Otimizador.Common.Domain.Models.Cenario;

// Cadastro simples do cenário. O CSV com as demandas é enviado depois, via upload dedicado
// (POST /Cenarios/{id}/csv), referenciando este cenário pelo Id.
public class CenarioCriacaoRequest
{
    public string Nome { get; set; }
    public List<string> ParametroIds { get; set; }
}
