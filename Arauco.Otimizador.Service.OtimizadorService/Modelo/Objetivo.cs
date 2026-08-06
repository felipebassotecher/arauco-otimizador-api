using Google.OrTools.Sat;

namespace Arauco.Otimizador.Service.OtimizadorService.Modelo;

public sealed class Termo
{
    public required string Nome { get; init; }
    public required string Descricao { get; init; }
    public required LinearExpr Expressao { get; init; }
    public int Ordem { get; set; }
    public long Peso { get; set; }
    public double Valor { get; set; }
}

public static class Objetivo
{
    public const int BaseRanking = 4;

    public static void AplicarPesos(IReadOnlyList<Termo> termos, ModoObjetivo modo)
    {
        if (modo == ModoObjetivo.Lexicografico)
        {
            foreach (var t in termos) t.Peso = 1;
            return;
        }

        var n = termos.Count;
        foreach (var t in termos)
        {
            var expoente = Math.Max(n - t.Ordem, 0);
            t.Peso = (long)Math.Pow(BaseRanking, expoente);
        }
    }

    public static LinearExpr Combinar(IReadOnlyList<Termo> termos) =>
        LinearExpr.Sum(termos.Where(t => t.Peso > 0).Select(t => t.Expressao * t.Peso));

    public static string? Diagnosticar(IReadOnlyList<Termo> termos, int semanas, ModoObjetivo modo)
    {
        var atender = termos.FirstOrDefault(t => t.Nome == "atender");
        var atraso = termos.FirstOrDefault(t => t.Nome == "atraso");
        var suavizacao = termos.FirstOrDefault(t => t.Nome == "suavizacao");
        var mixFrete = termos.FirstOrDefault(t => t.Nome == "mix_frete");
        if (atender is null || atraso is null) return null;

        if (modo == ModoObjetivo.Lexicografico)
            return atender.Ordem > 1
                ? $"lexicográfico: 'atender' ranqueado em {atender.Ordem}º — os níveis anteriores "
                  + "são otimizados primeiro e podem sacrificar atendimento. Escolha sua, registrada."
                : null;

        var custoNaoAtender = atender.Peso;
        var custoAtenderTarde = atraso.Peso * (semanas - 1) + (suavizacao?.Peso ?? 0)
                                + (mixFrete?.Peso ?? 0);

        return custoNaoAtender > custoAtenderTarde
            ? $"invariante OK: não atender ({custoNaoAtender:N0}) custa mais que atender tarde ({custoAtenderTarde:N0})"
            : $"ATENÇÃO: com esta ordem, NÃO ATENDER ({custoNaoAtender:N0}) sai mais barato que atender "
              + $"na última semana ({custoAtenderTarde:N0}). O otimizador vai descartar demanda de "
              + "propósito, mesmo com capacidade sobrando. Suba 'atender' na ordem se não for isso "
              + "que você quer.";
    }
}
