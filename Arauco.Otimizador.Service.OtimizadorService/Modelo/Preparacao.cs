using Arauco.Otimizador.Service.OtimizadorService.Dados;

namespace Arauco.Otimizador.Service.OtimizadorService.Modelo;

public enum MotivoExclusao
{
    ProdutoDesconhecido,
    SemElegibilidade,
    SemCapacidadeNaLinhaProduto,
    LoteMinimoMaiorQueDemanda,
}

public sealed record ItemExcluido(
    string ClienteId,
    string ProdutoId,
    int LinhaProdutoId,
    double VolumeM3,
    MotivoExclusao Motivo);

public sealed record Preparacao(
    IReadOnlyList<Item> Itens,
    IReadOnlyList<ItemExcluido> Excluidos,
    double DemandaTotalM3,
    double DemandaElegivelM3,
    double DemandaExcluidaM3);

public static class Preparador
{
    public static Preparacao Preparar(
        Carregador dados,
        IReadOnlyCollection<(int Centro, int LinhaProduto)> paresComCapacidade,
        Config config,
        List<string> notas)
    {
        var comCapacidade = paresComCapacidade.ToHashSet();

        var itens = new List<Item>();
        var excluidos = new List<ItemExcluido>();
        var indice = 0;
        var incotermsMistos = 0;
        var segmentosMistos = 0;

        var grupos = dados.Carteira
            .GroupBy(l => (l.ClienteId, l.ProdutoId))
            .OrderBy(g => g.Key.ClienteId).ThenBy(g => g.Key.ProdutoId);

        foreach (var grupo in grupos)
        {
            var (clienteId, produtoId) = grupo.Key;
            var volume = grupo.Sum(l => l.VolumeM3);
            var primeira = grupo.First();

            if (!dados.Produtos.TryGetValue(produtoId, out var produto))
            {
                excluidos.Add(new ItemExcluido(clienteId, produtoId, primeira.LinhaProdutoId,
                    volume, MotivoExclusao.ProdutoDesconhecido));
                continue;
            }

            if (!dados.Elegibilidade.TryGetValue(produtoId, out var centrosProduto) || centrosProduto.Count == 0)
            {
                excluidos.Add(new ItemExcluido(clienteId, produtoId, produto.LinhaProdutoId,
                    volume, MotivoExclusao.SemElegibilidade));
                continue;
            }

            var validos = centrosProduto
                .Where(c => comCapacidade.Contains((c, produto.LinhaProdutoId)))
                .ToList();

            if (validos.Count == 0)
            {
                excluidos.Add(new ItemExcluido(clienteId, produtoId, produto.LinhaProdutoId,
                    volume, MotivoExclusao.SemCapacidadeNaLinhaProduto));
                continue;
            }

            var chapa = produto.ChapaM3;
            var loteMinimoM3 = config.LoteMinimoEmChapas
                ? produto.LoteMinimoChapas * chapa
                : produto.LoteMinimoChapas;

            if (loteMinimoM3 > volume)
            {
                excluidos.Add(new ItemExcluido(clienteId, produtoId, produto.LinhaProdutoId,
                    volume, MotivoExclusao.LoteMinimoMaiorQueDemanda));
                continue;
            }

            var minimoSku = config.Carreta.MinimoSkuM3 ?? config.ChapasPorLote * chapa;
            var piso = Math.Min(Math.Max(loteMinimoM3, minimoSku), volume);

            var incoterms = primeira.Incoterms;
            var segmento = grupo.Select(l => l.Segmento).FirstOrDefault(s => !string.IsNullOrWhiteSpace(s)) ?? "";

            if (grupo.Select(l => l.Incoterms.StartsWith("CIF", StringComparison.OrdinalIgnoreCase))
                     .Distinct().Count() > 1)
                incotermsMistos++;
            if (grupo.Where(l => !string.IsNullOrWhiteSpace(l.Segmento))
                     .Select(l => l.Segmento.Contains("IND", StringComparison.OrdinalIgnoreCase))
                     .Distinct().Count() > 1)
                segmentosMistos++;

            itens.Add(new Item(
                Indice: indice++,
                ClienteId: clienteId,
                ClienteNome: primeira.ClienteNome,
                ProdutoId: produtoId,
                LinhaProdutoId: produto.LinhaProdutoId,
                VolumeM3: volume,
                DataDocumentoMaisAntiga: grupo.Min(l => l.DataDocumento),
                Cif: incoterms.StartsWith("CIF", StringComparison.OrdinalIgnoreCase),
                Industria: segmento.Contains("IND", StringComparison.OrdinalIgnoreCase),
                LoteMinimoM3: piso,
                CentrosElegiveis: validos,
                CarteiraIds: grupo.Select(l => l.CarteiraId).ToList()));
        }

        if (incotermsMistos > 0)
            notas.Add($"pre-flight: {incotermsMistos} item(ns) (cliente, produto) com linhas CIF e FOB "
                      + "misturadas — adotado o incoterm da primeira linha");
        if (segmentosMistos > 0)
            notas.Add($"pre-flight: {segmentosMistos} item(ns) com segmento divergente entre linhas "
                      + "— adotado o primeiro não vazio");

        var total = dados.Carteira.Sum(l => l.VolumeM3);
        var elegivel = itens.Sum(i => i.VolumeM3);

        return new Preparacao(itens, excluidos, total, elegivel, total - elegivel);
    }
}
