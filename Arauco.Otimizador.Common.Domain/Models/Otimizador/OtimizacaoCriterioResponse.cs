namespace Arauco.Otimizador.Common.Domain.Models.Otimizador;

public class OtimizacaoCriterioResponse
{
    public string Nome { get; set; }
    public string Descricao { get; set; }
    public int Ordem { get; set; }
    public long Peso { get; set; }
    public double Valor { get; set; }
}
