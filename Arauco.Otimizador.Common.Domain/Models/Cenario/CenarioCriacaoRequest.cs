namespace Arauco.Otimizador.Common.Domain.Models.Cenario;

// Enviado em POST /cenarios. Não contém dados do arquivo — o upload do CSV é feito em uma requisição
// separada (POST /cenarios/{id}/csv), após o cenário já existir (spec §3.4). `setupId` é obrigatório:
// o motor de otimização não tem mais configuração própria por cenário — horizonte, capacidade,
// carreta, limite de recebimento e ordem de importância dos critérios vêm do Setup vinculado (ver
// OtimizadorService.OtimizarAsync). O vínculo é definido aqui e não pode ser trocado depois.
public class CenarioCriacaoRequest
{
    public string Nome { get; set; }
    public string SetupId { get; set; }
}
