using Arauco.Otimizador.Data.Entities.Demanda;
using Arauco.Otimizador.Service.OtimizadorService.Dados;

namespace Arauco.Otimizador.Service.OtimizadorService.Mapeamento;

// Converte as demandas de um cenário (upload da carteira em aberto do ADC — ver DemandaCsvParser) para
// o formato LinhaCarteira do motor de otimização. Os campos vêm diretamente do arquivo carregado
// (CarteiraId, ClienteNome, DataDocumento, CentroOriginal); só LinhaProdutoId cai para o master data de
// Produto quando a linha não trouxer o valor (arquivo com coluna ausente/zerada).
public static class DemandaParaCarteiraMapper
{
    public static List<LinhaCarteira> Mapear(
        IReadOnlyList<Demanda> demandas,
        IReadOnlyDictionary<string, Produto> produtos)
    {
        var linhas = new List<LinhaCarteira>(demandas.Count);

        foreach (var d in demandas)
        {
            var linhaProdutoId = d.LinhaProdutoId > 0
                ? d.LinhaProdutoId
                : produtos.TryGetValue(d.Material, out var produto) ? produto.LinhaProdutoId : 0;

            linhas.Add(new LinhaCarteira(
                CarteiraId: d.CarteiraId,
                ClienteId: d.Cliente,
                ClienteNome: string.IsNullOrWhiteSpace(d.ClienteNome) ? d.Cliente : d.ClienteNome,
                ProdutoId: d.Material,
                LinhaProdutoId: linhaProdutoId,
                VolumeM3: (double)d.Volume,
                DataDocumento: d.DataDocumento,
                Incoterms: d.TipoFreteEnum.ToString().ToUpperInvariant(),
                Segmento: d.Segmento ?? "",
                CentroOriginal: d.CentroOriginal));
        }

        return linhas;
    }
}
