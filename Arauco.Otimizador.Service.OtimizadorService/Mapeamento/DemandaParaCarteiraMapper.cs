using Arauco.Otimizador.Data.Entities.Demanda;
using Arauco.Otimizador.Service.OtimizadorService.Dados;

namespace Arauco.Otimizador.Service.OtimizadorService.Mapeamento;

public static class DemandaParaCarteiraMapper
{
    public static List<LinhaCarteira> Mapear(
        IReadOnlyList<Demanda> demandas,
        IReadOnlyDictionary<string, Produto> produtos,
        IReadOnlyDictionary<string, IReadOnlyList<int>> elegibilidade)
    {
        var linhas = new List<LinhaCarteira>(demandas.Count);
        var id = 1L;

        foreach (var d in demandas)
        {
            var produtoId = d.Material;
            var centroOriginal = 0;

            if (elegibilidade.TryGetValue(produtoId, out var centros) && centros.Count > 0)
                centroOriginal = centros[0];
            else if (produtos.TryGetValue(produtoId, out var produto) &&
                     elegibilidade.TryGetValue(produto.ProdutoId, out var centrosProduto) &&
                     centrosProduto.Count > 0)
                centroOriginal = centrosProduto[0];

            var linhaProdutoId = produtos.TryGetValue(produtoId, out var p) ? p.LinhaProdutoId : 0;

            linhas.Add(new LinhaCarteira(
                CarteiraId: id++,
                ClienteId: d.Cliente,
                ClienteNome: d.Cliente,
                ProdutoId: produtoId,
                LinhaProdutoId: linhaProdutoId,
                VolumeM3: (double)d.Volume,
                DataDocumento: d.DataEntregaDesejada,
                Incoterms: d.TipoFreteEnum.ToString().ToUpperInvariant(),
                Segmento: d.SegmentoEnum.ToString().ToUpperInvariant(),
                CentroOriginal: centroOriginal));
        }

        return linhas;
    }
}
