using System.ComponentModel.DataAnnotations;
using Arauco.Otimizador.Common.Domain.Enums.Cartao;

namespace Arauco.Otimizador.DataApi.Models;

public class CartaoODataModel
{
    [Key]
    public string CartaoId { get; set; } = null!;
    public string Tipo { get; set; } = null!;
    public string TipoDescricao { get; set; } = null!;
    public DateTime DataHoraCriacao { get; set; }
    public string Mensagem { get; set; } = null!;

    // Remetente
    public int ColaboradorId_Remetente { get; set; }
    public string? Nome_Remetente { get; set; }
    public string? CentroCusto_Remetente { get; set; }
    public string? Cidade_Remetente { get; set; }
    public string? Cargo_Remetente { get; set; }

    // Destinatario
    public int? ColaboradorId_Destinatario { get; set; }
    public string? Nome_Destinatario { get; set; }
    public string? CentroCusto_Destinatario { get; set; }
    public string? Cidade_Destinatario { get; set; }
    public string? Cargo_Destinatario { get; set; }

    public string? NomeDestinatarioPersonalizado { get; set; }

    public static string GetTipoDescricao(CartaoTipoEnum tipo)
    {
        return tipo switch
        {
            CartaoTipoEnum.Seguranca => "Segurança",
            CartaoTipoEnum.ExcelenciaEInovacao => "Excelência e Inovação",
            CartaoTipoEnum.TrabalhoEmEquipe => "Trabalho em Equipe",
            CartaoTipoEnum.BomCidadao => "Bom Cidadão",
            CartaoTipoEnum.Compromisso => "Compromisso",
            _ => tipo.ToString()
        };
    }
}
