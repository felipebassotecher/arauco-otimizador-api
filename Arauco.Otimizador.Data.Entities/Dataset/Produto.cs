namespace Arauco.Otimizador.Data.Entities.Dataset;

// Master data de produtos, consumida pelo motor de otimização (Dados/Carregador.cs) no lugar do
// antigo produtos.parquet — ver Data/Datasets/ (arquivo mantido só como referência histórica).
public class Produto
{
    public string ProdutoId { get; set; }
    public string Descricao { get; set; }
    public int LinhaProdutoId { get; set; }
    public double LoteMinimoChapas { get; set; }
    public double LarguraMm { get; set; }
    public double ComprimentoMm { get; set; }
    public double EspessuraMm { get; set; }
    public bool Ativo { get; set; }
}
