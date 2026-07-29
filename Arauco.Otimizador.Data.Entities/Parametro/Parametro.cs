namespace Arauco.Otimizador.Data.Entities.Parametro;

public class Parametro
{
    public string ParametroId { get; set; }
    public string Nome { get; set; }
    public string Chave { get; set; }
    public string Descricao { get; set; }
    public double Peso { get; set; }
    public bool Ativo { get; set; }
}
