using System.Runtime.Serialization;

namespace Arauco.Otimizador.Common.Domain.Enums.Setup;

public enum CriterioOrdemEnum
{
    [EnumMember(Value = "priorizar_frete_cif")]
    PriorizarFreteCIF = 1,

    [EnumMember(Value = "piorizar_cliente_revenda")]
    PiorizarClienteRevenda = 2,

    [EnumMember(Value = "antecipar")]
    Antecipar = 3,

    [EnumMember(Value = "atender_demanda")]
    AtenderDemanda = 4,

    [EnumMember(Value = "pedido_mais_antigo")]
    PedidoMaisAntigo = 5
}
