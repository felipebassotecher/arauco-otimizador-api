namespace Arauco.Otimizador.Data.Entities.Parametro;

public class ParametroValor
{
    public int Id { get; set; }
    public string ParametroId { get; set; }
    public string Valor { get; set; }
    public string Rotulo { get; set; }
    public double? Peso { get; set; }
}
