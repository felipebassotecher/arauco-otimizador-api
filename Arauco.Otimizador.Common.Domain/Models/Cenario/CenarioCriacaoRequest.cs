using Arauco.Otimizador.Common.Domain.Models.Criterio;

namespace Arauco.Otimizador.Common.Domain.Models.Cenario;

// Enviado em POST /cenarios. Não contém dados do arquivo — o upload do CSV é feito em uma requisição
// separada (POST /cenarios/{id}/csv), após o cenário já existir (spec §3.4). `criterios` é a lista de
// regras (criterioChave + operador + valor + peso) configuradas na criação; o mesmo criterioChave pode
// se repetir mais de uma vez na lista.
public class CenarioCriacaoRequest
{
    public string Nome { get; set; }
    public List<CriterioRegraRequest> Criterios { get; set; }
}