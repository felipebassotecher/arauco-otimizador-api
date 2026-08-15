using Arauco.Otimizador.Common.Domain.Enums.Setup;

namespace Arauco.Otimizador.Data.Entities.Setup;

public class Setup
{
    public string SetupId { get; set; }
    public string Nome { get; set; }
    public string? Descricao { get; set; }
    public decimal? PesoMinimoCarregamento { get; set; }
    public int? PercentualVariacaoMediaVenda { get; set; }
    public int? QuantidadeMaximaTrocas { get; set; }
    public bool UtilizarToleranciaPeso { get; set; }
    public bool PermitirCarregarAbaixoPesoMinimo { get; set; }
    public bool PriorizarTipoFrete { get; set; }
    public TipoFreteEnum? TipoFreteEnum { get; set; }
    public bool PriorizarTipoCliente { get; set; }
    public TipoClienteEnum? TipoClienteEnum { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime? DataAlteracao { get; set; }
}
