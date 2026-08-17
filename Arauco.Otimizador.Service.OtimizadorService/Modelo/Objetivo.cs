using Arauco.Otimizador.Common.Domain.Enums.Setup;
using Arauco.Otimizador.Data.Entities.Setup;

namespace Arauco.Otimizador.Service.OtimizadorService.Modelo;

public sealed record TermoObjetivo(CriterioOrdemEnum Criterio, int Ordem, long Peso);

// Converte a ordem de importância do setup vinculado ao cenário (SetupOrdemImportancia) em pesos
// para o objetivo do CP-SAT — modo Ranking: um único solve, peso por critério = BaseRanking^(N-ordem)
// (critério mais importante pesa exponencialmente mais). Critérios inativos (Ativo=false) ficam de
// fora do ranking — não geram termo no objetivo (ver Otimizacao.Resolver).
public static class Objetivo
{
    // Base pequena de propósito: com 5 critérios, base 4 chega a no máximo 4^4=256 no mais
    // importante, mantendo a magnitude do objetivo tratável mesmo somada a volumes em décimos de m³
    // (mesma preocupação documentada no projeto de referência otimizador-teste-entrega).
    public const int BaseRanking = 4;

    public static List<TermoObjetivo> CalcularPesos(IReadOnlyList<SetupOrdemImportancia> ordemImportancia)
    {
        var ativos = ordemImportancia
            .Where(o => o.Ativo)
            .OrderBy(o => o.Ordem)
            .ToList();

        var n = ativos.Count;

        return ativos
            .Select((o, indice) =>
            {
                var ordem = indice + 1;
                var peso = (long)Math.Pow(BaseRanking, Math.Max(n - ordem, 0));
                return new TermoObjetivo(o.CriterioEnum, ordem, peso);
            })
            .ToList();
    }
}
