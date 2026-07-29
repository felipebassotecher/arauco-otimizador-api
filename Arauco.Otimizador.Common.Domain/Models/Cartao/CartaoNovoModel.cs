using Arauco.Otimizador.Common.Domain.Enums.Cartao;

namespace Arauco.Otimizador.Common.Domain.Models.Cartao;

public class CartaoNovoModel
{
    public int? DestinatarioId { get; set; }
    public string? NomeSocial { get; set; }
    public string? NomeColaboradorExterno { get; set; }
    public CartaoTipoEnum Tipo { get; set; }
    public string Mensagem { get; set; }
}
