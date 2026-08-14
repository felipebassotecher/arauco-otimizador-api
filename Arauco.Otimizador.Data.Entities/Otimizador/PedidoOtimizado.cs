using Arauco.Otimizador.Common.Domain.Enums.Demanda;

namespace Arauco.Otimizador.Data.Entities.Otimizador;

public class PedidoOtimizado
{
    public string PedidoId { get; set; }
    public string CenarioId { get; set; }
    public string ResultadoId { get; set; }
    public string Cliente { get; set; }
    public string Material { get; set; }
    public int LinhaProdutoId { get; set; }
    public int CentroId { get; set; }
    public string Centro { get; set; }
    public TipoFreteEnum TipoFreteEnum { get; set; }
    public decimal Volume { get; set; }
    public int Ano { get; set; }
    public int Semana { get; set; }
    public bool Pinado { get; set; }
    public int ScorePeso { get; set; }
}
