using Arauco.Otimizador.Common.Domain.Enums.Demanda;
using System.Globalization;
using System.Text;

namespace Arauco.Otimizador.Common.Domain.Util;

// Compartilhado entre CenarioService (criação) e DemandaService (upload) para não duplicar o parse do CSV.
//
// Formato esperado: extração da "Carteira em aberto" do ADC (mesma extração usada como referência em
// otimizador-teste-entrega/sql/extracao/demanda.sql), com cabeçalho e as colunas (qualquer ordem):
// carteira_id, cliente_id, cliente_nome, produto_id, linha_produto_id, volume_m3, data_documento,
// incoterms, segmento, centro_original, data_solicitacao_remessa. As demais colunas da extração
// (mes, ano, carteira_m3, faturado_m3, status_credito, tipo_documento_venda, numero_pedido, vendedor,
// regiao, prioridade_remessa) são aceitas no arquivo mas ignoradas — o motor de otimização portado de
// otimizador-teste-entrega não as consome (ver analise/CONTINUIDADE.md do projeto de referência).
public static class DemandaCsvParser
{
    public record LinhaDemanda(
        long CarteiraId,
        string Cliente,
        string ClienteNome,
        string Material,
        int LinhaProdutoId,
        decimal Volume,
        DateTime DataDocumento,
        DateTime DataEntrega,
        TipoFreteEnum TipoFrete,
        string Segmento,
        int CentroOriginal);

    private static readonly string[] DateFormats = { "yyyy-MM-dd", "dd/MM/yyyy", "MM/dd/yyyy" };

    private static readonly string[] ColunasObrigatorias = { "cliente_id", "produto_id", "volume_m3", "data_documento" };

    // Linhas que não parseiam como uma demanda válida (volume <= 0, data obrigatória ausente/inválida
    // etc.) são ignoradas silenciosamente, mesmo critério já usado pelo Carregador de referência.
    public static List<LinhaDemanda> Parse(string conteudoCsv)
    {
        var linhas = new List<LinhaDemanda>();

        if (string.IsNullOrWhiteSpace(conteudoCsv))
            return linhas;

        var rows = conteudoCsv
            .Split('\n')
            .Select(l => l.TrimEnd('\r'))
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToList();

        if (rows.Count < 2)
            return linhas;

        var cabecalho = SplitCsvLine(rows[0]).Select(c => c.Trim().ToLowerInvariant()).ToList();
        var indice = new Dictionary<string, int>();
        for (var i = 0; i < cabecalho.Count; i++)
            indice[cabecalho[i]] = i;

        if (ColunasObrigatorias.Any(c => !indice.ContainsKey(c)))
            return linhas;

        string? Coluna(IReadOnlyList<string> colunas, string nome) =>
            indice.TryGetValue(nome, out var i) && i < colunas.Count ? colunas[i].Trim() : null;

        for (var linha = 1; linha < rows.Count; linha++)
        {
            var colunas = SplitCsvLine(rows[linha]);

            var clienteId = Coluna(colunas, "cliente_id");
            var produtoId = Coluna(colunas, "produto_id");

            if (string.IsNullOrWhiteSpace(clienteId) || string.IsNullOrWhiteSpace(produtoId))
                continue;

            if (!decimal.TryParse(Coluna(colunas, "volume_m3"), NumberStyles.Any, CultureInfo.InvariantCulture, out var volume) || volume <= 0)
                continue;

            if (!TryParseData(Coluna(colunas, "data_documento"), out var dataDocumento))
                continue;

            if (!TryParseData(Coluna(colunas, "data_solicitacao_remessa"), out var dataEntrega))
                dataEntrega = dataDocumento;

            var incoterms = Coluna(colunas, "incoterms") ?? "";
            var tipoFrete = incoterms.StartsWith("CIF", StringComparison.OrdinalIgnoreCase)
                ? TipoFreteEnum.CIF
                : TipoFreteEnum.FOB;

            var segmento = Coluna(colunas, "segmento") ?? "";
            var clienteNome = Coluna(colunas, "cliente_nome");

            _ = long.TryParse(Coluna(colunas, "carteira_id"), NumberStyles.Any, CultureInfo.InvariantCulture, out var carteiraId);
            _ = int.TryParse(Coluna(colunas, "linha_produto_id"), NumberStyles.Any, CultureInfo.InvariantCulture, out var linhaProdutoId);
            _ = int.TryParse(Coluna(colunas, "centro_original"), NumberStyles.Any, CultureInfo.InvariantCulture, out var centroOriginal);

            linhas.Add(new LinhaDemanda(
                carteiraId,
                clienteId,
                string.IsNullOrWhiteSpace(clienteNome) ? clienteId : clienteNome,
                produtoId,
                linhaProdutoId,
                volume,
                dataDocumento,
                dataEntrega,
                tipoFrete,
                segmento,
                centroOriginal));
        }

        return linhas;
    }

    private static bool TryParseData(string? texto, out DateTime data)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            data = default;
            return false;
        }

        return DateTime.TryParseExact(texto, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out data) ||
               DateTime.TryParse(texto, CultureInfo.InvariantCulture, DateTimeStyles.None, out data);
    }

    // Split de uma linha CSV respeitando campos entre aspas (RFC 4180: vírgula e quebra de linha dentro
    // de aspas não separam coluna; aspas duplicadas "" dentro de um campo viram uma aspa literal). A
    // extração do ADC traz nome de cliente com vírgula (ex.: "FORMICA DISTRIBUTION CENTER, S.A."), que
    // um split ingênuo por vírgula quebraria.
    private static List<string> SplitCsvLine(string linha)
    {
        var campos = new List<string>();
        var atual = new StringBuilder();
        var dentroDeAspas = false;

        for (var i = 0; i < linha.Length; i++)
        {
            var c = linha[i];

            if (dentroDeAspas)
            {
                if (c == '"')
                {
                    if (i + 1 < linha.Length && linha[i + 1] == '"')
                    {
                        atual.Append('"');
                        i++;
                    }
                    else
                    {
                        dentroDeAspas = false;
                    }
                }
                else
                {
                    atual.Append(c);
                }
                continue;
            }

            switch (c)
            {
                case '"':
                    dentroDeAspas = true;
                    break;
                case ',':
                    campos.Add(atual.ToString());
                    atual.Clear();
                    break;
                default:
                    atual.Append(c);
                    break;
            }
        }

        campos.Add(atual.ToString());
        return campos;
    }
}
