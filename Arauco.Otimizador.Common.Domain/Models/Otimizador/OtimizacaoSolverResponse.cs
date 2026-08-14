namespace Arauco.Otimizador.Common.Domain.Models.Otimizador;

public class OtimizacaoSolverResponse
{
    public string Status { get; set; }
    public double Segundos { get; set; }
    public double Objetivo { get; set; }
    public int Variaveis { get; set; }
    public int Binarias { get; set; }
}
