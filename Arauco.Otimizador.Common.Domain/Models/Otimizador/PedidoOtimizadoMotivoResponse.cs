using Arauco.Otimizador.Common.Domain.Enums.Otimizador;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Arauco.Otimizador.Common.Domain.Models.Otimizador;

public class PedidoOtimizadoMotivoResponse
{
    [JsonConverter(typeof(StringEnumConverter))]
    public CategoriaMotivoEnum Categoria { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    public MotivoAlocacaoEnum Motivo { get; set; }
}
