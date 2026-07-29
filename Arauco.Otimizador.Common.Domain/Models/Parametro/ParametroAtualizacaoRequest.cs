namespace Arauco.Otimizador.Common.Domain.Models.Parametro;

// Enviado em PUT /Parametros/{id}. Sem Id — vem da rota.
public class ParametroAtualizacaoRequest
{
    public string Nome { get; set; }
    public string Chave { get; set; }
    public string Descricao { get; set; }
    public double Peso { get; set; }
    public bool Ativo { get; set; }
    public List<ParametroValorRequest>? Valores { get; set; }
}
