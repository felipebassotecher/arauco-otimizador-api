using Arauco.Otimizador.Common.Domain.Enums.Demanda;

namespace Arauco.Otimizador.Data.Entities.Otimizador;

public class PedidoOtimizado
{
    public string PedidoId { get; set; }
    public string CenarioId { get; set; }
    public string ResultadoId { get; set; }
    // Liga o pedido de volta à demanda (Demanda.CarteiraId) que o originou — usado para excluir com
    // precisão, numa reotimização, a demanda cujo pedido já está pinado (ver
    // OtimizadorService.DescontarPinados). Sem isso, duas demandas do mesmo cliente+produto seriam
    // indistinguíveis para esse desconto.
    public long CarteiraId { get; set; }
    public string Cliente { get; set; }
    public string Material { get; set; }
    public int LinhaProdutoId { get; set; }
    public int CentroId { get; set; }
    public string Centro { get; set; }
    public TipoFreteEnum TipoFreteEnum { get; set; }
    // Tipo de cliente (Indústria/Revenda), mesma resolução usada pelo critério "Tipo de Cliente"
    // (AvaliadorCriterios/Item.Industria) — ver Preparacao.cs.
    public bool Industria { get; set; }
    public decimal Volume { get; set; }
    public int Ano { get; set; }
    public int Semana { get; set; }
    public bool Pinado { get; set; }
    public int ScorePeso { get; set; }
}
