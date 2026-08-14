using Arauco.Otimizador.Common.Domain.Enums.Demanda;
using System.Globalization;

namespace Arauco.Otimizador.Common.Domain.Util;

// Compartilhado entre CenarioService (criação) e DemandaService (upload) para não duplicar o parse do CSV.
public static class DemandaCsvParser
{
    public record LinhaDemanda(string Cliente, string Material, decimal Volume, DateTime DataEntrega, TipoFreteEnum TipoFrete, SegmentoEnum Segmento);

    private static readonly string[] DateFormats = { "dd/MM/yyyy", "yyyy-MM-dd", "MM/dd/yyyy" };

    // Formato esperado: Cliente,Material,Volume,DataEntrega,TipoFrete,Segmento (a 6ª coluna é opcional —
    // CSV antigo de 5 colunas assume Revenda, mesmo default que já era hardcoded antes do campo existir).
    // Linhas que não parseiam como uma demanda válida (ex.: cabeçalho) são ignoradas silenciosamente.
    public static List<LinhaDemanda> Parse(string conteudoCsv)
    {
        var linhas = new List<LinhaDemanda>();

        if (string.IsNullOrWhiteSpace(conteudoCsv))
            return linhas;

        var rows = conteudoCsv
            .Split('\n')
            .Select(l => l.Trim().TrimEnd('\r'))
            .Where(l => !string.IsNullOrWhiteSpace(l));

        foreach (var row in rows)
        {
            var colunas = row.Split(',');

            if (colunas.Length < 4)
                continue;

            if (!decimal.TryParse(colunas[2].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var volume))
                continue;

            if (!DateTime.TryParseExact(colunas[3].Trim(), DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dataEntrega) &&
                !DateTime.TryParse(colunas[3].Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out dataEntrega))
                continue;

            var tipoFrete = colunas.Length > 4 && colunas[4].Trim().Equals("CIF", StringComparison.OrdinalIgnoreCase)
                ? TipoFreteEnum.CIF
                : TipoFreteEnum.FOB;

            var segmento = colunas.Length > 5 && colunas[5].Trim().Equals("INDUSTRIA", StringComparison.OrdinalIgnoreCase)
                ? SegmentoEnum.Industria
                : SegmentoEnum.Revenda;

            linhas.Add(new LinhaDemanda(colunas[0].Trim(), colunas[1].Trim(), volume, dataEntrega, tipoFrete, segmento));
        }

        return linhas;
    }
}
