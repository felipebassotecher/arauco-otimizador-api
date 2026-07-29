using Arauco.Otimizador.Common.Domain.Enums.Cartao;

namespace Arauco.Otimizador.Common.Domain.Models.Cartao;

public class CartaoDetalheModel
{
    public string Id { get; set; }
    public string Mensagem { get; set; }
    public CartaoTipoEnum Tipo { get; set; }
    public CartaoStatusEnum Status { get; set; }
    public DateTime DataHoraCriacao { get; set; }
    public KeyValuePair<int, string> Remetente { get; set; }
    public KeyValuePair<int, string>? Destinatario { get; set; }
    public string? NomeDestinatarioPersonalizado { get; set; }
}
