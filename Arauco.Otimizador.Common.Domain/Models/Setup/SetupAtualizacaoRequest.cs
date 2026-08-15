namespace Arauco.Otimizador.Common.Domain.Models.Setup;

public class SetupAtualizacaoRequest
{
    public string Nome { get; set; }
    public string? Descricao { get; set; }
    public decimal? VolumeMinimoCarreta { get; set; }
    public decimal? VolumeMaximoCarreta { get; set; }
    public int? QuantidadeMinimaSkuPorLote { get; set; }
    public int? CapacidadeMaximaRecebimentoCliente { get; set; }
    public int? MixTipoFrete { get; set; }
    public List<SetupOrdemImportanciaRequest> OrdemImportancia { get; set; }
}
