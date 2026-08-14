namespace Arauco.Otimizador.Data.Entities.Dataset;

// Master data de elegibilidade produto x centro, consumida pelo motor de otimização
// (Dados/Carregador.cs) no lugar do antigo elegibilidade.parquet — ver Data/Datasets/ (arquivo
// mantido só como referência histórica).
public class Elegibilidade
{
    public int Id { get; set; }
    public string ProdutoId { get; set; }
    public int CentroId { get; set; }
}
