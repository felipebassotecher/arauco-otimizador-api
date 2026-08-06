namespace Arauco.Otimizador.Data.Entities.Otimizador;

public class OtimizacaoCriterio
{
    public string CriterioId { get; set; }
    public string ResultadoId { get; set; }
    public string Nome { get; set; }
    public string Descricao { get; set; }
    public int Ordem { get; set; }
    public long Peso { get; set; }
    public decimal Valor { get; set; }
}
