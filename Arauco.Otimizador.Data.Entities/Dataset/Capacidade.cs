namespace Arauco.Otimizador.Data.Entities.Dataset;

// Master data de capacidade semanal por centro/linha, consumida pelo motor de otimização
// (Dados/Carregador.cs) no lugar do antigo capacidade.parquet — ver Data/Datasets/ (arquivo mantido
// só como referência histórica).
public class Capacidade
{
    public int Id { get; set; }
    public int CentroId { get; set; }
    public int LinhaProducaoId { get; set; }
    public int LinhaProdutoId { get; set; }
    public int SemanaIso { get; set; }
    public int Ano { get; set; }
    public int Mes { get; set; }
    public int TipoAlocacao { get; set; }
    public long Quantidade { get; set; }
    public DateTime? DataCriacao { get; set; }
}
