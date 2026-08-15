using System.Runtime.Serialization;

namespace Arauco.Otimizador.Common.Domain.Enums.Setup;

public enum CriterioOrdemEnum
{
    [EnumMember(Value = "priorizar_cliente_revenda")]
    PriorizarClienteRevenda = 1,

    [EnumMember(Value = "menor_prazo_entrega")]
    MenorPrazoEntrega = 2,

    [EnumMember(Value = "maior_volume")]
    MaiorVolume = 3,

    [EnumMember(Value = "menor_distancia")]
    MenorDistancia = 4,

    [EnumMember(Value = "maior_margem")]
    MaiorMargem = 5
}
