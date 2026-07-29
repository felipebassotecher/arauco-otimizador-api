namespace Arauco.Otimizador.Common.Domain.Models.Parametro;

// Retornado por GET /Parametros/{id}.
public class ParametroDetalheResponse
{
    public string Id { get; set; }
    public string Nome { get; set; }
    public string Chave { get; set; }
    public string Descricao { get; set; }
    public double Peso { get; set; }
    public bool Ativo { get; set; }
    public List<ParametroValorResponse>? Valores { get; set; }
}
