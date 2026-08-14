using Arauco.Otimizador.Common.Domain.Enums.Criterio;
using Arauco.Otimizador.Common.Domain.Util;
using Arauco.Otimizador.Data.Entities.Cenario;
using Arauco.Otimizador.Service.OtimizadorService.Dados;

namespace Arauco.Otimizador.Service.OtimizadorV2Service.CriteriosV2;

// Converte os critérios personalizados do cenário (CenarioCriterio: chave + operador + valor + peso,
// persistidos via CriteriosDisponiveis — spec §3.9) em um score por item (cliente+produto), somando o
// peso de toda regra que casa. Ex.: TipoFrete=CIF (peso 15) + TipoCliente=INDUSTRIA (peso 25) => +40
// para um item CIF de cliente indústria que casa nas duas regras.
public static class AvaliadorCriterios
{
    public static int SomarPesos(Item item, IReadOnlyList<CenarioCriterio> regras)
    {
        var soma = 0;

        foreach (var regra in regras)
            if (Casa(item, regra))
                soma += regra.Peso;

        return soma;
    }

    private static bool Casa(Item item, CenarioCriterio regra)
    {
        var chave = CriteriosDisponiveis.ObterChaveEnum(regra.CriterioChave);
        if (chave is null) return false;

        var valorCampo = ObterValorCampo(item, chave.Value);
        if (valorCampo is null) return false;

        return regra.Operador switch
        {
            OperadorCriterioEnum.IgualA => valorCampo.Equals(regra.Valor, StringComparison.OrdinalIgnoreCase),
            OperadorCriterioEnum.DiferenteDe => !valorCampo.Equals(regra.Valor, StringComparison.OrdinalIgnoreCase),
            OperadorCriterioEnum.ComecaCom => valorCampo.StartsWith(regra.Valor, StringComparison.OrdinalIgnoreCase),
            OperadorCriterioEnum.TerminaCom => valorCampo.EndsWith(regra.Valor, StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }

    // Item (Modelo/Preparacao.cs do V1) já resolve o Incoterm/Segmento representativo do grupo
    // cliente+produto em Cif/Industria (booleanos) — reaproveita essa resolução em vez de reler a
    // Demanda bruta, mantendo o mesmo critério de desempate que o pré-flight do V1 já usa para grupos
    // com linhas mistas (spec: adota a primeira linha não vazia).
    private static string? ObterValorCampo(Item item, CriterioChaveEnum chave) => chave switch
    {
        CriterioChaveEnum.TipoFrete => item.Cif ? "CIF" : "FOB",
        CriterioChaveEnum.TipoCliente => item.Industria ? "INDUSTRIA" : "REVENDA",
        _ => null
    };
}
