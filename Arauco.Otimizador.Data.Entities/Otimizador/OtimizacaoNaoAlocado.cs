namespace Arauco.Otimizador.Data.Entities.Otimizador;

public class OtimizacaoNaoAlocado
{
    public string NaoAlocadoId { get; set; }
    public string ResultadoId { get; set; }
    public string Cliente { get; set; }
    public string Produto { get; set; }
    public int LinhaProdutoId { get; set; }
    public decimal VolumeM3 { get; set; }
    public decimal DemandaM3 { get; set; }
    public long Prioridade { get; set; }
    public string Motivo { get; set; }
    public decimal MaiorFolgaM3 { get; set; }
    public decimal PisoM3 { get; set; }
}
