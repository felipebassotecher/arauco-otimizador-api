using Arauco.Otimizador.Common.Domain.Enums.Demanda;
using Arauco.Otimizador.Data.Entities.Demanda;
using Arauco.Otimizador.Service.OtimizadorService;
using Arauco.Otimizador.Service.OtimizadorService.Dados;
using Arauco.Otimizador.Service.OtimizadorService.Mapeamento;
using Arauco.Otimizador.Service.OtimizadorService.Modelo;

var pastaDatasets = LocalizarDatasets();
Console.WriteLine($"Datasets: {pastaDatasets}");

var executor = new Executor();
var carregador = await executor.CarregarAsync(pastaDatasets);

Console.WriteLine();
Console.WriteLine("== Master data (parquet) ==");
Console.WriteLine($"Produtos: {carregador.Produtos.Count}");
Console.WriteLine($"Centros: {carregador.Centros.Count}");
Console.WriteLine($"Pares centro/linha-produto elegiveis: {carregador.ParesElegiveis.Count}");
Console.WriteLine($"Carteira real (demanda.parquet): {carregador.Carteira.Count} linha(s)");
foreach (var nota in carregador.Notas) Console.WriteLine($"  nota: {nota}");

var config = new Config { Horizonte = 4, LimiteSegundos = 20 };

Console.WriteLine();
Console.WriteLine("== Cenario 1: motor rodando direto sobre a carteira real dos parquet ==");
Executar(executor, carregador, config);

Console.WriteLine();
Console.WriteLine("== Cenario 2: mesmo fluxo do OtimizadorService (Demanda -> DemandaParaCarteiraMapper) ==");
var demandas = CriarDemandasDeExemplo(carregador);
var carteiraMapeada = DemandaParaCarteiraMapper.Mapear(demandas, carregador.Produtos, carregador.Elegibilidade);
var dadosMapeados = carregador.ComCarteira(carteiraMapeada);
Executar(executor, dadosMapeados, config);

return;

static void Executar(Executor executor, Carregador dados, Config config)
{
    var execucao = executor.Executar(dados, config);

    Console.WriteLine($"Status solver: {execucao.Solver.Status} ({execucao.Solver.Segundos:0.00}s, {execucao.Solver.Variaveis} vars, {execucao.Solver.Binarias} binarias)");
    Console.WriteLine($"Demanda total: {execucao.Resumo.DemandaTotalM3:N2} m3 | elegivel: {execucao.Resumo.DemandaElegivelM3:N2} m3");
    Console.WriteLine($"Capacidade: {execucao.Resumo.CapacidadeTotal:N0} (fator aplicado {execucao.Resumo.FatorCapacidade:P0})");
    Console.WriteLine($"Alocado: {execucao.Resumo.AlocadoM3:N2} m3 ({execucao.Resumo.PercentualAlocado:P1}) | Nao alocado: {execucao.Resumo.NaoAlocadoM3:N2} m3");
    Console.WriteLine($"Itens: {execucao.Resumo.Itens} | Excluidos no pre-flight: {execucao.Resumo.ItensExcluidos}");
    Console.WriteLine($"Alocacoes: {execucao.Alocacoes.Count} | Saldos nao alocados: {execucao.NaoAlocado.Count} | Embarques: {execucao.Carretas.TotalEmbarques}");
    foreach (var nota in execucao.Notas) Console.WriteLine($"  nota: {nota}");
}

static List<Demanda> CriarDemandasDeExemplo(Carregador carregador)
{
    var produtosElegiveis = carregador.Produtos.Values
        .Where(p => carregador.Elegibilidade.ContainsKey(p.ProdutoId))
        .Take(5)
        .ToList();

    if (produtosElegiveis.Count == 0)
        throw new InvalidOperationException("Nenhum produto elegivel encontrado nos datasets para montar o cenario de exemplo.");

    var rnd = new Random(42);
    var demandas = new List<Demanda>();

    for (var i = 0; i < produtosElegiveis.Count; i++)
    {
        var produto = produtosElegiveis[i];
        demandas.Add(new Demanda
        {
            DemandaId = $"TST{i:D3}",
            CenarioId = "TESTE1",
            Cliente = $"CLIENTE_TESTE_{i + 1}",
            Material = produto.ProdutoId,
            Volume = (decimal)(10 + rnd.NextDouble() * 40),
            DataEntregaDesejada = DateTime.Today.AddDays(7 + i * 3),
            TipoFreteEnum = i % 2 == 0 ? TipoFreteEnum.CIF : TipoFreteEnum.FOB
        });
    }

    return demandas;
}

static string LocalizarDatasets()
{
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir is not null)
    {
        var candidato = Path.Combine(dir.FullName, "Data", "Datasets");
        if (File.Exists(Path.Combine(candidato, "produtos.parquet")))
            return candidato;
        dir = dir.Parent;
    }

    throw new DirectoryNotFoundException(
        "Nao encontrei Data/Datasets subindo a partir de " + AppContext.BaseDirectory);
}
