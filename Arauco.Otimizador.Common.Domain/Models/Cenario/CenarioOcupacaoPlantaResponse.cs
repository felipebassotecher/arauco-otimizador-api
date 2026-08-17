namespace Arauco.Otimizador.Common.Domain.Models.Cenario;

// Ocupação de cada planta (Centro), somada sobre todo o horizonte do cenário (a janela de semanas
// coberta pelos pedidos otimizados dele — da primeira à última semana com pedido alocado). A
// capacidade é a capacidade real declarada (`Capacidade`, importada do ADC) somada por planta ao
// longo dessas semanas — não é a capacidade calibrada/simulada que o motor de otimização efetivamente
// usou para decidir a alocação (essa é específica de cada execução e não fica persistida), então este
// número é uma leitura "capacidade declarada x volume alocado", não uma reconstrução exata do que o
// solver viu.
public class CenarioOcupacaoPlantaResponse
{
    public int CentroId { get; set; }
    public string Centro { get; set; }
    public double CapacidadeTotalM3 { get; set; }
    public double VolumeAlocadoM3 { get; set; }
    public double Percentual { get; set; }
}
