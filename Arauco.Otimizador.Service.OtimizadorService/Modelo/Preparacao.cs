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

// Cada linha de LinhaCarteira (= cada Demanda) vira exatamente um Item — sem agrupar por
// (cliente, produto). Isso garante que uma demanda gere, ao final da otimização, no máximo um
// pedido: Otimizacao.cs também aloca cada Item inteiro em um único bucket (centro, semana), nunca
// dividido entre vários.
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

        var linhas = dados.Carteira
            .OrderBy(l => l.ClienteId).ThenBy(l => l.ProdutoId).ThenBy(l => l.CarteiraId);

        foreach (var linha in linhas)
        {
            var clienteId = linha.ClienteId;
            var produtoId = linha.ProdutoId;
            var volume = linha.VolumeM3;

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

            itens.Add(new Item(
                Indice: indice++,
                ClienteId: clienteId,
                ClienteNome: linha.ClienteNome,
                ProdutoId: produtoId,
                LinhaProdutoId: produto.LinhaProdutoId,
                VolumeM3: volume,
                DataDocumentoMaisAntiga: linha.DataDocumento,
                Cif: linha.Incoterms.StartsWith("CIF", StringComparison.OrdinalIgnoreCase),
                Industria: linha.Segmento.Contains("IND", StringComparison.OrdinalIgnoreCase),
                LoteMinimoM3: loteMinimoM3,
                CentrosElegiveis: validos,
                CarteiraIds: [linha.CarteiraId]));
        }

        var total = dados.Carteira.Sum(l => l.VolumeM3);
        var elegivel = itens.Sum(i => i.VolumeM3);

        return new Preparacao(itens, excluidos, total, elegivel, total - elegivel);
    }
}
