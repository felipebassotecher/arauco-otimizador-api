namespace Arauco.Otimizador.Data.Entities.Dataset;

// Nome legível de cada linha de produto (Produto.LinhaProdutoId/Capacidade.LinhaProdutoId já usam
// esse id, mas sem nome associado) — extraída da coluna linha_produto_nome de produtos.parquet, com
// distinct por linha_produto_id (ver Data/Datasets/, arquivo mantido só como referência histórica).
public class LinhaProduto
{
    public int LinhaProdutoId { get; set; }
    public string Nome { get; set; }
}
