namespace Arauco.Otimizador.Common.Domain.Models.OtimizadorV2;

public class OtimizacaoV2SolverResponse
{
    public string Status { get; set; }
    public double Segundos { get; set; }
    public double Objetivo { get; set; }
    public int Variaveis { get; set; }
    public int Binarias { get; set; }
}
