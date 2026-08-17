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

// Agrupa as linhas de carteira por (cliente, produto) em um único Item, somando volume e mantendo
// todos os CarteiraIds do grupo — um item pode agregar várias demandas do mesmo cliente+produto.
// Otimizacao.cs decide, por item, se ele é divisível (pode se espalhar por vários buckets) ou
// indivisível (tudo-ou-nada), com base no piso calculado aqui.
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
        var gruposMistos = 0;

        var grupos = dados.Carteira
            .GroupBy(l => (l.ClienteId, l.ProdutoId))
            .OrderBy(g => g.Key.ClienteId).ThenBy(g => g.Key.ProdutoId);

        foreach (var grupo in grupos)
        {
            var linhas = grupo.OrderBy(l => l.CarteiraId).ToList();
            var clienteId = grupo.Key.ClienteId;
            var produtoId = grupo.Key.ProdutoId;
            var volume = linhas.Sum(l => l.VolumeM3);

            if (!dados.Produtos.TryGetValue(produtoId, out var produto))
            {
                excluidos.Add(new ItemExcluido(clienteId, produtoId, 0,
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

            var minimoSkuM3 = (config.QuantidadeMinimaSkuPorLote ?? config.ChapasPorLote) * chapa;
            var piso = Math.Min(Math.Max(loteMinimoM3, minimoSkuM3), volume);

            var primeira = linhas[0];
            if (linhas.Any(l => l.Incoterms != primeira.Incoterms || l.Segmento != primeira.Segmento))
                gruposMistos++;

            itens.Add(new Item(
                Indice: indice++,
                ClienteId: clienteId,
                ClienteNome: primeira.ClienteNome,
                ProdutoId: produtoId,
                LinhaProdutoId: produto.LinhaProdutoId,
                VolumeM3: volume,
                DataDocumentoMaisAntiga: linhas.Min(l => l.DataDocumento),
                Cif: primeira.Incoterms.StartsWith("CIF", StringComparison.OrdinalIgnoreCase),
                Industria: primeira.Segmento.Contains("IND", StringComparison.OrdinalIgnoreCase),
                Piso: piso,
                CentrosElegiveis: validos,
                CarteiraIds: linhas.Select(l => l.CarteiraId).ToList()));
        }

        if (gruposMistos > 0)
            notas.Add($"preparação: {gruposMistos} grupo(s) cliente+produto com incoterm/segmento "
                      + "divergente entre linhas — adotados os valores da linha mais antiga (menor CarteiraId)");

        var total = dados.Carteira.Sum(l => l.VolumeM3);
        var elegivel = itens.Sum(i => i.VolumeM3);

        return new Preparacao(itens, excluidos, total, elegivel, total - elegivel);
    }
}
