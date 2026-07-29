namespace Arauco.Otimizador.Common.Domain.Models.Parametro;

// Retornado por POST /Parametros.
public class ParametroCriacaoResponse
{
    public string Id { get; set; }
    public string Nome { get; set; }
    public string Chave { get; set; }
    public string Descricao { get; set; }
    public double Peso { get; set; }
    public bool Ativo { get; set; }
    public List<ParametroValorResponse>? Valores { get; set; }
}
