namespace Arauco.Otimizador.Service.OtimizadorService.Dados;

public sealed class ParametrosCarteira
{
    public int Seed { get; set; } = 42;
    public int Clientes { get; set; } = 300;
    public int Itens { get; set; } = 6000;
    public double VolumeMinimoM3 { get; set; } = 1;
    public double VolumeMaximoM3 { get; set; } = 120;
    public double FracaoCif { get; set; } = 0.53;
    public double FracaoIndustria { get; set; } = 0.53;
    public int DispersaoDias { get; set; } = 180;
    public double ViesMultiPlanta { get; set; } = 0.8;
}

public static class GeradorCarteira
{
    private static readonly string[] Incoterms = ["CIF", "FOB"];

    public static List<LinhaCarteira> Gerar(
        Carregador master, ParametrosCarteira p, List<string> notas)
    {
        var rnd = new Random(p.Seed);

        var candidatos = master.Produtos.Values
            .Where(x => master.Elegibilidade.ContainsKey(x.ProdutoId))
            .ToList();

        if (candidatos.Count == 0)
            throw new InvalidOperationException("Master data sem produtos elegíveis — nada a gerar.");

        var multiPlanta = candidatos
            .Where(x => master.Elegibilidade[x.ProdutoId].Count > 1)
            .ToList();

        if (multiPlanta.Count == 0)
        {
            notas.Add("gerador: nenhum produto com elegibilidade multi-planta — o viés de disputa não terá efeito neste master data");
            multiPlanta = candidatos;
        }

        var clientes = Enumerable.Range(1, p.Clientes)
            .Select(i => (Id: $"SINT{i:D5}", Nome: $"CLIENTE SINTETICO {i}"))
            .ToList();

        var referencia = DateTime.Today;
        var linhas = new List<LinhaCarteira>(p.Itens);
        var usados = new HashSet<(string, string)>();

        for (var k = 0; k < p.Itens; k++)
        {
            var cliente = clientes[rnd.Next(clientes.Count)];
            var fonte = rnd.NextDouble() < p.ViesMultiPlanta ? multiPlanta : candidatos;
            var produto = fonte[rnd.Next(fonte.Count)];

            if (!usados.Add((cliente.Id, produto.ProdutoId))) continue;

            var u = rnd.NextDouble();
            var volume = p.VolumeMinimoM3
                         * Math.Pow(p.VolumeMaximoM3 / p.VolumeMinimoM3, u);

            var cif = rnd.NextDouble() < p.FracaoCif;
            var industria = rnd.NextDouble() < p.FracaoIndustria;

            linhas.Add(new LinhaCarteira(
                CarteiraId: k + 1,
                ClienteId: cliente.Id,
                ClienteNome: cliente.Nome,
                ProdutoId: produto.ProdutoId,
                LinhaProdutoId: produto.LinhaProdutoId,
                VolumeM3: Math.Round(volume, 2),
                DataDocumento: referencia.AddDays(-rnd.Next(p.DispersaoDias)),
                Incoterms: cif ? Incoterms[0] : Incoterms[1],
                Segmento: industria ? "INDUSTRIA" : "REVENDA",
                CentroOriginal: master.Elegibilidade[produto.ProdutoId][0]));
        }

        var volumeTotal = linhas.Sum(l => l.VolumeM3);
        var comEscolha = linhas.Count(l => master.Elegibilidade[l.ProdutoId].Count > 1);

        notas.Add($"carteira SINTETICA (seed {p.Seed}): {linhas.Count} itens, "
                  + $"{linhas.Select(l => l.ClienteId).Distinct().Count()} clientes, "
                  + $"{volumeTotal:N0} m3, {comEscolha / (double)linhas.Count:P0} com elegibilidade "
                  + "multi-planta. Master data (produtos, elegibilidade, capacidade) continua REAL.");

        return linhas;
    }
}
