using Arauco.Otimizador.Common.Domain.Enums.Setup;

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
    // Parâmetros do motor de otimização (espelham Config.cs) — lidos pelo motor a partir do Setup
    // vinculado ao Cenário (Cenario.SetupId) — ver OtimizadorService.CriarConfig.
    public int? Horizonte { get; set; }
    public ModoCapacidadeEnum? ModoCapacidade { get; set; }
    public string? SemanaInicial { get; set; }
    public decimal? AlvoCapacidadeSobreDemanda { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime? DataAlteracao { get; set; }
}
