namespace Arauco.Otimizador.Data.Entities.Dataset;

// Master data de centros/plantas, consumida pelo motor de otimização (Dados/Carregador.cs) no lugar
// do antigo centros.parquet — ver Data/Datasets/ (arquivo mantido só como referência histórica).
public class Centro
{
    public int CentroId { get; set; }
    public string Codigo { get; set; }
    public string Nome { get; set; }
    public bool Ativo { get; set; }
    public int PorcentagemIndustria { get; set; }
    public int PorcentagemRevenda { get; set; }
}
