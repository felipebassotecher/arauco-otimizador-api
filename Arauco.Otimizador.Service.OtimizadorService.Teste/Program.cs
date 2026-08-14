using Arauco.Otimizador.Common.Domain.Enums.Criterio;
using Arauco.Otimizador.Common.Domain.Enums.Demanda;
using Arauco.Otimizador.Data.Entities.Cenario;
using Arauco.Otimizador.Data.Entities.Demanda;
using Arauco.Otimizador.Service.OtimizadorService;
using Arauco.Otimizador.Service.OtimizadorService.Capacidade;
using Arauco.Otimizador.Service.OtimizadorService.Dados;
using Arauco.Otimizador.Service.OtimizadorService.Mapeamento;
using Arauco.Otimizador.Service.OtimizadorService.Modelo;
using Arauco.Otimizador.Service.OtimizadorV2Service.CriteriosV2;
using Arauco.Otimizador.Service.OtimizadorV2Service.Modelo;

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

Console.WriteLine();
Console.WriteLine("== Cenario V2: criterios personalizados (banco) + pinning ==");
await RodarCenarioV2Async(carregador);

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
            TipoFreteEnum = i % 2 == 0 ? TipoFreteEnum.CIF : TipoFreteEnum.FOB,
            SegmentoEnum = i % 2 == 0 ? SegmentoEnum.Revenda : SegmentoEnum.Industria
        });
    }

    return demandas;
}

static async Task RodarCenarioV2Async(Carregador carregadorBase)
{
    var produtosElegiveis = carregadorBase.Produtos.Values
        .Where(p => carregadorBase.Elegibilidade.ContainsKey(p.ProdutoId))
        .Take(4)
        .ToList();

    if (produtosElegiveis.Count == 0)
        throw new InvalidOperationException("Nenhum produto elegivel encontrado nos datasets para montar o cenario V2 de exemplo.");

    var rnd = new Random(7);
    var demandas = new List<Demanda>();

    for (var i = 0; i < produtosElegiveis.Count; i++)
    {
        var produto = produtosElegiveis[i];
        demandas.Add(new Demanda
        {
            DemandaId = $"V2T{i:D3}",
            CenarioId = "TESTEV2",
            Cliente = $"CLIENTE_V2_{i + 1}",
            Material = produto.ProdutoId,
            Volume = (decimal)(15 + rnd.NextDouble() * 30),
            DataEntregaDesejada = DateTime.Today.AddDays(7 + i * 3),
            TipoFreteEnum = i % 2 == 0 ? TipoFreteEnum.CIF : TipoFreteEnum.FOB,
            SegmentoEnum = i % 3 == 0 ? SegmentoEnum.Industria : SegmentoEnum.Revenda
        });
    }

    // Mesmo exemplo citado no pedido: TipoFrete=CIF -> peso 15, TipoCliente=INDUSTRIA -> peso 25.
    var criterios = new List<CenarioCriterio>
    {
        new() { CenarioId = "TESTEV2", CriterioChave = "tipoFrete", Operador = OperadorCriterioEnum.IgualA, Valor = "CIF", Peso = 15 },
        new() { CenarioId = "TESTEV2", CriterioChave = "tipoCliente", Operador = OperadorCriterioEnum.IgualA, Valor = "INDUSTRIA", Peso = 25 }
    };

    var carteira = DemandaParaCarteiraMapper.Mapear(demandas, carregadorBase.Produtos, carregadorBase.Elegibilidade);
    var dados = carregadorBase.ComCarteira(carteira);

    var config = new Config { Horizonte = 4, LimiteSegundos = 15 };
    var notas = new List<string>(dados.Notas);

    var bruta = ProvedorCapacidade.MontarBruta(config, dados.Capacidade, dados.ParesElegiveis, notas);
    var prep = Preparador.Preparar(dados, bruta.Pares, config, notas);
    var demandaPorLinha = prep.Itens.GroupBy(i => i.LinhaProdutoId).ToDictionary(g => g.Key, g => g.Sum(i => i.VolumeM3));
    var capacidade = ProvedorCapacidade.Aplicar(config, bruta, demandaPorLinha, notas);

    Console.WriteLine($"Itens preparados: {prep.Itens.Count} | excluidos no pre-flight: {prep.Excluidos.Count}");
    foreach (var item in prep.Itens)
    {
        var score = AvaliadorCriterios.SomarPesos(item, criterios);
        Console.WriteLine($"  item {item.Indice}: cliente={item.ClienteId} produto={item.ProdutoId} cif={item.Cif} industria={item.Industria} volume={item.VolumeM3:N2} somaPesos={score}");
    }

    var resultado1 = OtimizacaoV2.Resolver(config, prep.Itens, capacidade, criterios, notas);
    Console.WriteLine($"[sem pin] status={resultado1.Status} alocacoes={resultado1.Alocacoes.Count} naoAlocado={resultado1.NaoAlocadoPorItem.Values.Sum():N2} m3");
    foreach (var a in resultado1.Alocacoes)
        Console.WriteLine($"  alocado: item={a.ItemIndice} centro={a.CentroId} semana={capacidade.Semanas[a.IndiceSemana]} volume={a.VolumeM3:N2} scorePeso={a.ScorePeso}");

    if (resultado1.Alocacoes.Count == 0)
    {
        Console.WriteLine("(nenhuma alocacao para simular pin)");
        return;
    }

    // Simula o pinning: desconta o volume da 1a alocacao do item e do bucket de capacidade antes de
    // reotimizar — mesma logica que OtimizadorV2Service.DescontarPinados aplica com dados do banco.
    var pin = resultado1.Alocacoes[0];
    var itemPinado = prep.Itens.First(i => i.Indice == pin.ItemIndice);

    var itensAjustados = prep.Itens
        .Select(item => item.Indice == pin.ItemIndice
            ? item with { VolumeM3 = Math.Max(0, item.VolumeM3 - pin.VolumeM3) }
            : item)
        .ToList();

    var novoPorBucket = new Dictionary<(int, int, int), long>(capacidade.PorBucket);
    var chaveBucket = (pin.CentroId, itemPinado.LinhaProdutoId, pin.IndiceSemana);
    novoPorBucket[chaveBucket] = Math.Max(0, novoPorBucket.GetValueOrDefault(chaveBucket, 0) - (long)Math.Round(pin.VolumeM3));
    var capacidadeAjustada = capacidade with { PorBucket = novoPorBucket };

    var notas2 = new List<string>();
    var resultado2 = OtimizacaoV2.Resolver(config, itensAjustados, capacidadeAjustada, criterios, notas2);

    Console.WriteLine($"[com pin] item {pin.ItemIndice} fixado em centro {pin.CentroId}/semana {capacidade.Semanas[pin.IndiceSemana]} ({pin.VolumeM3:N2} m3)");
    Console.WriteLine($"  status={resultado2.Status} alocacoes={resultado2.Alocacoes.Count} naoAlocado={resultado2.NaoAlocadoPorItem.Values.Sum():N2} m3");

    var volumeReotimizadoDoItemPinado = resultado2.Alocacoes.Where(a => a.ItemIndice == pin.ItemIndice).Sum(a => a.VolumeM3);
    Console.WriteLine($"  volume do item pinado reotimizado na 2a rodada: {volumeReotimizadoDoItemPinado:N2} m3 (esperado <= volume original menos o que foi pinado)");

    foreach (var a in resultado2.Alocacoes)
        Console.WriteLine($"  alocado: item={a.ItemIndice} centro={a.CentroId} semana={capacidadeAjustada.Semanas[a.IndiceSemana]} volume={a.VolumeM3:N2}");
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
