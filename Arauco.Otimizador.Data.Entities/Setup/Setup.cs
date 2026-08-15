namespace Arauco.Otimizador.Data.Entities.Setup;

public class Setup
{
    public string SetupId { get; set; }
    public string Nome { get; set; }
    public string? Descricao { get; set; }
    public decimal? VolumeMinimoCarreta { get; set; }
    public decimal? VolumeMaximoCarreta { get; set; }
    public int? QuantidadeMinimaSkuPorLote { get; set; }
    public int? CapacidadeMaximaRecebimentoCliente { get; set; }
    public int? MixTipoFrete { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime? DataAlteracao { get; set; }
}
