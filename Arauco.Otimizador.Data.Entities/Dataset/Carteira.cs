namespace Arauco.Otimizador.Data.Entities.Dataset;

// Master data de carteira real (cliente+produto+volume), consumida pelo motor de otimização
// (Dados/Carregador.cs) no lugar do antigo demanda.parquet — ver Data/Datasets/ (arquivo mantido só
// como referência histórica). Não confundir com Demanda.Demanda (volume digitado/importado por
// cenário via CSV).
public class Carteira
{
    public long CarteiraId { get; set; }
    public string ClienteId { get; set; }
    public string ClienteNome { get; set; }
    public string ProdutoId { get; set; }
    public int LinhaProdutoId { get; set; }
    public decimal VolumeM3 { get; set; }
    public DateTime? DataDocumento { get; set; }
    public string Incoterms { get; set; }
    public string Segmento { get; set; }
    public long CentroOriginal { get; set; }
}
