namespace Arauco.Otimizador.Common.Domain.Models.Parametro;

// Enviado em POST /Parametros. Sem Id — gerado pelo servidor.
public class ParametroCriacaoRequest
{
    public string Nome { get; set; }
    public string Chave { get; set; }
    public string Descricao { get; set; }
    public double Peso { get; set; }
    public bool Ativo { get; set; }
    public List<ParametroValorRequest>? Valores { get; set; }
}
