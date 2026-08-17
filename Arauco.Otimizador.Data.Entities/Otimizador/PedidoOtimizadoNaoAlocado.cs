using Arauco.Otimizador.Common.Domain.Enums.Otimizador;

namespace Arauco.Otimizador.Data.Entities.Otimizador;

public class PedidoOtimizadoNaoAlocado
{
    public string NaoAlocadoId { get; set; }
    public string ResultadoId { get; set; }
    public string Cliente { get; set; }
    public string Material { get; set; }
    public int LinhaProdutoId { get; set; }
    public decimal VolumeM3 { get; set; }
    public string Motivo { get; set; }
    public CategoriaMotivoEnum? CategoriaEnum { get; set; }
    public MotivoAlocacaoEnum? MotivoEnum { get; set; }
}
