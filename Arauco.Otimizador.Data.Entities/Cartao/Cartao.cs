using Arauco.Otimizador.Common.Domain.Enums.Cartao;

namespace Arauco.Otimizador.Data.Entities.Cartao;

public class Cartao
{
    public string CartaoId { get; set; }
    public CartaoTipoEnum TipoEnum { get; set; }
    public DateTime DataHoraCriacao { get; set; }
    public string Mensagem { get; set; }

    // Remetente
    public int ColaboradorId_Remetente { get; set; }
    public string? Nome_Remetente { get; set; }

    // Destinatario
    public int? ColaboradorId_Destinatario { get; set; }
    public string? Nome_Destinatario { get; set; }


    // Nome de colaborador externo (sem colaboradorId) ou nome social (personalizado)
    public string? NomeDestinatarioPersonalizado { get; set; }
}
