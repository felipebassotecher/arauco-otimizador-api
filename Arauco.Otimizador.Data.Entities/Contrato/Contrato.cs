using Arauco.Otimizador.Common.Domain.Enums.Demanda;

namespace Arauco.Otimizador.Data.Entities.Contrato;

// Contrato obtido via integração com o Google Cloud Platform (BigQuery/Dataform) — ver
// Service.ContratoService. Não é uma tabela do banco local; a entidade só dá forma tipada ao
// resultado das consultas enquanto ele é montado e mapeado para ContratoResponse.
public class Contrato
{
    public string ContratoId { get; set; }
    public string ClienteId { get; set; }
    public string ClienteNome { get; set; }
    public TipoFreteEnum TipoFreteEnum { get; set; }
}
