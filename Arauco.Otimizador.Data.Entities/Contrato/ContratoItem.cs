namespace Arauco.Otimizador.Data.Entities.Contrato;

// Item de um Contrato (produto + volume), obtido via integração com o GCP — ver Contrato.cs.
public class ContratoItem
{
    public string ContratoItemId { get; set; }
    public string ContratoId { get; set; }
    public string ProdutoId { get; set; }
    public string ProdutoNome { get; set; }
    public decimal Volume { get; set; }
}
