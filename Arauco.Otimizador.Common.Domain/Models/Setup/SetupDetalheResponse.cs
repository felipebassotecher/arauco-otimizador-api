using Arauco.Otimizador.Common.Domain.Enums.Setup;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Arauco.Otimizador.Common.Domain.Models.Setup;

public class SetupDetalheResponse
{
    public string Id { get; set; }
    public string Nome { get; set; }
    public string? Descricao { get; set; }
    public decimal? PesoMinimoCarregamento { get; set; }
    public int? PercentualVariacaoMediaVenda { get; set; }
    public int? QuantidadeMaximaTrocas { get; set; }
    public bool UtilizarToleranciaPeso { get; set; }
    public bool PermitirCarregarAbaixoPesoMinimo { get; set; }
    public bool PriorizarTipoFrete { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    public TipoFreteEnum? TipoFrete { get; set; }

    public bool PriorizarTipoCliente { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    public TipoClienteEnum? TipoCliente { get; set; }

    public List<SetupOrdemImportanciaResponse> OrdemImportancia { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime? DataAlteracao { get; set; }
}
