using Arauco.Otimizador.Common.Domain.Enums.Setup;

namespace Arauco.Otimizador.Common.Domain.Models.Setup;

public class SetupAtualizacaoRequest
{
    public string Nome { get; set; }
    public string? Descricao { get; set; }
    public decimal? PesoMinimoCarregamento { get; set; }
    public int? PercentualVariacaoMediaVenda { get; set; }
    public int? QuantidadeMaximaTrocas { get; set; }
    public bool UtilizarToleranciaPeso { get; set; }
    public bool PermitirCarregarAbaixoPesoMinimo { get; set; }
    public bool PriorizarTipoFrete { get; set; }
    public TipoFreteEnum? TipoFrete { get; set; }
    public bool PriorizarTipoCliente { get; set; }
    public TipoClienteEnum? TipoCliente { get; set; }
    public List<SetupOrdemImportanciaRequest> OrdemImportancia { get; set; }
}
