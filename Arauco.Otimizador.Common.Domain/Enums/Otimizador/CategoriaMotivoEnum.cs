using System.Runtime.Serialization;

namespace Arauco.Otimizador.Common.Domain.Enums.Otimizador;

// Categorias de "por que este pedido/item ficou onde ficou", exibidas no detalhe do pedido
// (front-end). Um pedido alocado sempre tem 1+ motivo de PorqueNestaSemana e exatamente 1 motivo
// de PorqueNesteCentro — ver Otimizacao.cs (ComputarMotivosSemana/ComputarMotivoCentro). Um item
// não alocado tem exatamente 1 motivo de PorqueNaoAlocado.
public enum CategoriaMotivoEnum
{
    [EnumMember(Value = "porque_nesta_semana")]
    PorqueNestaSemana,

    [EnumMember(Value = "porque_neste_centro")]
    PorqueNesteCentro,

    [EnumMember(Value = "porque_nao_alocado")]
    PorqueNaoAlocado
}
