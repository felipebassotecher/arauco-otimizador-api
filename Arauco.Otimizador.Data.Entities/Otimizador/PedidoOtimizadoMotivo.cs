using Arauco.Otimizador.Common.Domain.Enums.Otimizador;

namespace Arauco.Otimizador.Data.Entities.Otimizador;

// Um pedido alocado pode ter mais de um motivo na categoria PorqueNestaSemana (ex.: sem
// capacidade em algumas semanas anteriores E lote mínimo não cabendo em outras) — por isso é uma
// linha por (pedido, motivo), não colunas fixas em PedidoOtimizado.
public class PedidoOtimizadoMotivo
{
    public int Id { get; set; }
    public string PedidoId { get; set; }
    public CategoriaMotivoEnum CategoriaEnum { get; set; }
    public MotivoAlocacaoEnum MotivoEnum { get; set; }
}
