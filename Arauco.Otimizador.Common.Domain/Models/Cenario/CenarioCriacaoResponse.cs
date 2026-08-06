namespace Arauco.Otimizador.Common.Domain.Models.Cenario;

// Retornado por POST /cenarios. O front só usa o identificador do cenário recém-criado, para
// redirecionar à tela de detalhes — por isso não retorna a representação completa (spec §3.4.1).
public class CenarioCriacaoResponse
{
    public string Id { get; set; }
}