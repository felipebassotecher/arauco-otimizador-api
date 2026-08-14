namespace Arauco.Otimizador.Common.Domain.Models.Otimizador;

public class OtimizacaoRequest
{
    public int? Horizonte { get; set; }
    public int? Capacidade { get; set; }
    public string? SemanaInicial { get; set; }
    public double? AlvoCapacidadeSobreDemanda { get; set; }
    public double? LimiteSegundos { get; set; }
    public double? CarretaMinimoM3 { get; set; }
    public double? CarretaMaximoM3 { get; set; }
    public int? LimiteRecebimentoCarretasPorSemana { get; set; }
}
