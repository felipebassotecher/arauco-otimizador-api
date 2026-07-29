namespace Arauco.Otimizador.Common.Domain.Models.Parametro;

// Retornado por GET /Parametros e GET /Parametros/ativos. Também reaproveitado como o formato
// "resumo" de parâmetro embutido em CenarioDetalheResponse/CenarioCriacaoResponse/etc.
public class ParametroListaResponse
{
    public string Id { get; set; }
    public string Nome { get; set; }
    public string Chave { get; set; }
    public string Descricao { get; set; }
    public double Peso { get; set; }
    public bool Ativo { get; set; }
    public List<ParametroValorResponse>? Valores { get; set; }
}
