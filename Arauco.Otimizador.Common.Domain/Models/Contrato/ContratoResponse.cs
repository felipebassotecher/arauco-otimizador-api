namespace Arauco.Otimizador.Common.Domain.Models.Contrato;

// Retornado por POST /cenarios/{id}/enriquecer — um item por contrato encontrado no GCP.
public class ContratoResponse
{
    public string ClienteId { get; set; }
    public string ClienteNome { get; set; }
    public string TipoFrete { get; set; }
    public List<ContratoItemResponse> Itens { get; set; } = [];
}
