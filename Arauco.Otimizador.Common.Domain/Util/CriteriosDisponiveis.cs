using Arauco.Otimizador.Common.Domain.Enums.Criterio;

namespace Arauco.Otimizador.Common.Domain.Util;

// Catálogo fixo (em código) dos critérios disponíveis e seu tipo de dado. Não é um cadastro de banco
// (spec §1/§3.9/§5.9). Ao adicionar um critério novo no futuro, inclua um valor em CriterioChaveEnum
// e uma entrada aqui (com chave string, nome e tipo) — sem criar tabela de "critérios disponíveis".
// É a fonte única de verdade: chave enum ↔ chave string ↔ nome legível ↔ tipo. Usado também para
// validar a compatibilidade operador×tipo (spec §3.1/§5.10) e para servir GET /cenarios/criterios-disponiveis.
public static class CriteriosDisponiveis
{
    public record Criterio(CriterioChaveEnum Chave, string ChaveString, string Nome, TipoCriterioEnum Tipo);

    private static readonly List<Criterio> Itens = new()
    {
        new Criterio(CriterioChaveEnum.TipoFrete, "tipoFrete", "Tipo de Frete", TipoCriterioEnum.String)
    };

    public static IReadOnlyList<Criterio> Todos => Itens;

    public static TipoCriterioEnum? ObterTipo(CriterioChaveEnum chave)
    {
        return Itens.FirstOrDefault(c => c.Chave == chave)?.Tipo;
    }

    public static string ObterNome(CriterioChaveEnum chave)
    {
        return Itens.FirstOrDefault(c => c.Chave == chave)?.Nome ?? chave.ToString();
    }

    // enum -> string (valor de chave persistido na coluna CenarioCriterio.CriterioChave, ex.: "tipoFrete")
    public static string ObterChaveString(CriterioChaveEnum chave)
    {
        return Itens.FirstOrDefault(c => c.Chave == chave)?.ChaveString ?? chave.ToString();
    }

    // string -> enum (leitura da coluna CenarioCriterio.CriterioChave)
    public static CriterioChaveEnum? ObterChaveEnum(string chaveString)
    {
        return Itens.FirstOrDefault(c => c.ChaveString == chaveString)?.Chave;
    }

    // Valida se o operador é aplicável ao tipo do critério (spec §3.1 — tabela OperadorCriterioEnum).
    public static bool OperadorCompativel(TipoCriterioEnum tipo, OperadorCriterioEnum operador)
    {
        return tipo switch
        {
            TipoCriterioEnum.String => operador is OperadorCriterioEnum.IgualA
                or OperadorCriterioEnum.DiferenteDe
                or OperadorCriterioEnum.ComecaCom
                or OperadorCriterioEnum.TerminaCom,

            TipoCriterioEnum.Numerico => operador is OperadorCriterioEnum.IgualA
                or OperadorCriterioEnum.DiferenteDe
                or OperadorCriterioEnum.MaiorQue
                or OperadorCriterioEnum.MenorQue,

            _ => false
        };
    }
}