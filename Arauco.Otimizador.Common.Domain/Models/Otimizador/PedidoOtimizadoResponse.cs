namespace Arauco.Otimizador.Common.Domain.Models.Otimizador;

public class PedidoOtimizadoResponse
{
    public string Id { get; set; }
    public string Cliente { get; set; }
    public string Material { get; set; }
    public int LinhaProdutoId { get; set; }
    public int CentroId { get; set; }
    public string Centro { get; set; }
    public string TipoFrete { get; set; }
    public string TipoCliente { get; set; }
    public decimal Volume { get; set; }
    public int Ano { get; set; }
    public int Semana { get; set; }
    public bool Pinado { get; set; }
    public int ScorePeso { get; set; }
}
