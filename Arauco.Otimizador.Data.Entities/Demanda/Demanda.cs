using Arauco.Otimizador.Common.Domain.Enums.Demanda;

namespace Arauco.Otimizador.Data.Entities.Demanda;

public class Demanda
{
    public string DemandaId { get; set; }
    public string CenarioId { get; set; }
    public long CarteiraId { get; set; }
    public string Cliente { get; set; }
    public string ClienteNome { get; set; }
    public string Material { get; set; }
    public int LinhaProdutoId { get; set; }
    public decimal Volume { get; set; }
    public DateTime DataDocumento { get; set; }
    public DateTime DataEntregaDesejada { get; set; }
    public TipoFreteEnum TipoFreteEnum { get; set; }
    public string Segmento { get; set; }
    public int CentroOriginal { get; set; }
}
