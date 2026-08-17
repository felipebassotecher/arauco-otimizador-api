using Arauco.Otimizador.Common.Domain.Enums.Demanda;
using Arauco.Otimizador.Common.Domain.Enums.Setup;
using Arauco.Otimizador.Data.Entities.Demanda;
using Arauco.Otimizador.Data.Entities.Setup;
using Arauco.Otimizador.Data.MySql;
using Arauco.Otimizador.Service.OtimizadorService;
using Arauco.Otimizador.Service.OtimizadorService.Capacidade;
using Arauco.Otimizador.Service.OtimizadorService.Dados;
using Arauco.Otimizador.Service.OtimizadorService.Mapeamento;
using Arauco.Otimizador.Service.OtimizadorService.Modelo;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using AppDbContext = Arauco.Otimizador.Data.MySql.DbContext;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var connString = OptionsHelper.GetConnectionString(configuration);
var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseMySql(connString, ServerVersion.AutoDetect(connString))
    .Options;
await using var context = new AppDbContext(options);
var unitOfWork = new UnitOfWork(context);

var executor = new Executor();
var carregador = await executor.CarregarAsync(unitOfWork);

Console.WriteLine();
Console.WriteLine("== Master data (banco) ==");
Console.WriteLine($"Produtos: {carregador.Produtos.Count}");
Console.WriteLine($"Centros: {carregador.Centros.Count}");
Console.WriteLine($"Pares centro/linha-produto elegiveis: {carregador.ParesElegiveis.Count}");
Console.WriteLine($"Carteira real (banco): {carregador.Carteira.Count} linha(s)");
foreach (var nota in carregador.Notas) Console.WriteLine($"  nota: {nota}");

Console.WriteLine();
Console.WriteLine("== Cenario: motor CP-SAT (criterios personalizados do cenario + pinning) ==");
await RodarCenarioAsync(carregador);

return;

static async Task RodarCenarioAsync(Carregador carregadorBase)
{
    var produtosElegiveis = carregadorBase.Produtos.Values
        .Where(p => carregadorBase.Elegibilidade.ContainsKey(p.ProdutoId))
        .Take(4)
        .ToList();

    if (produtosElegiveis.Count == 0)
        throw new InvalidOperationException("Nenhum produto elegivel encontrado nos datasets para montar o cenario de exemplo.");

    var rnd = new Random(7);
    var demandas = new List<Demanda>();

    for (var i = 0; i < produtosElegiveis.Count; i++)
    {
        var produto = produtosElegiveis[i];
        demandas.Add(new Demanda
        {
            DemandaId = $"TSTDEMANDA{i:D2}",
            CenarioId = "TESTE1",
            CarteiraId = i + 1,
            Cliente = $"CLIENTE_TESTE_{i + 1}",
            ClienteNome = $"Cliente Teste {i + 1}",
            Material = produto.ProdutoId,
            LinhaProdutoId = produto.LinhaProdutoId,
            Volume = (decimal)(15 + rnd.NextDouble() * 30),
            DataDocumento = DateTime.Today.AddDays(-i * 5),
            DataEntregaDesejada = DateTime.Today.AddDays(7 + i * 3),
            TipoFreteEnum = i % 2 == 0 ? TipoFreteEnum.CIF : TipoFreteEnum.FOB,
            Segmento = i % 3 == 0 ? "INDUSTRIA" : "REVENDA",
            CentroOriginal = 0
        });
    }

    // Ordem de importância de exemplo (mesmo espírito do exemplo antigo: industria e CIF priorizados).
    var ordemImportancia = new List<SetupOrdemImportancia>
    {
        new() { SetupId = "TESTE1", CriterioEnum = CriterioOrdemEnum.PiorizarClienteRevenda, Ordem = 1, Ativo = true },
        new() { SetupId = "TESTE1", CriterioEnum = CriterioOrdemEnum.PriorizarFreteCIF, Ordem = 2, Ativo = true },
        new() { SetupId = "TESTE1", CriterioEnum = CriterioOrdemEnum.AtenderDemanda, Ordem = 3, Ativo = true }
    };
    var termos = Objetivo.CalcularPesos(ordemImportancia);

    var carteira = DemandaParaCarteiraMapper.Mapear(demandas, carregadorBase.Produtos);
    var dados = carregadorBase.ComCarteira(carteira);

    var config = new Config { Horizonte = 4, LimiteSegundos = 15 };
    var notas = new List<string>(dados.Notas);

    var bruta = ProvedorCapacidade.MontarBruta(config, dados.Capacidade, dados.ParesElegiveis, notas);
    var prep = Preparador.Preparar(dados, bruta.Pares, config, notas);
    var demandaPorLinha = prep.Itens.GroupBy(i => i.LinhaProdutoId).ToDictionary(g => g.Key, g => g.Sum(i => i.VolumeM3));
    var capacidade = ProvedorCapacidade.Aplicar(config, bruta, demandaPorLinha, notas);

    Console.WriteLine($"Itens preparados: {prep.Itens.Count} | excluidos no pre-flight: {prep.Excluidos.Count}");
    foreach (var item in prep.Itens)
        Console.WriteLine($"  item {item.Indice}: cliente={item.ClienteId} produto={item.ProdutoId} cif={item.Cif} industria={item.Industria} volume={item.VolumeM3:N2} piso={item.Piso:N2}");

    var resultado1 = Otimizacao.Resolver(config, prep.Itens, capacidade, termos, notas);
    Console.WriteLine($"[sem pin] status={resultado1.Status} alocacoes={resultado1.Alocacoes.Count} naoAlocado={resultado1.NaoAlocadoPorItem.Values.Sum():N2} m3");
    foreach (var nota in notas) Console.WriteLine($"  nota: {nota}");
    foreach (var a in resultado1.Alocacoes)
        Console.WriteLine($"  alocado: item={a.ItemIndice} centro={a.CentroId} semana={capacidade.Semanas[a.IndiceSemana]} volume={a.VolumeM3:N2} scorePeso={a.ScorePeso}");

    if (resultado1.Alocacoes.Count == 0)
    {
        Console.WriteLine("(nenhuma alocacao para simular pin)");
        return;
    }

    // Simula o pinning: desconta o volume da 1a alocacao do item e do bucket de capacidade antes de
    // reotimizar — mesma logica que OtimizadorService.DescontarPinados aplica com dados do banco.
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
    var resultado2 = Otimizacao.Resolver(config, itensAjustados, capacidadeAjustada, termos, notas2);
    foreach (var nota in notas2) Console.WriteLine($"  nota: {nota}");

    Console.WriteLine($"[com pin] item {pin.ItemIndice} fixado em centro {pin.CentroId}/semana {capacidade.Semanas[pin.IndiceSemana]} ({pin.VolumeM3:N2} m3)");
    Console.WriteLine($"  status={resultado2.Status} alocacoes={resultado2.Alocacoes.Count} naoAlocado={resultado2.NaoAlocadoPorItem.Values.Sum():N2} m3");

    var volumeReotimizadoDoItemPinado = resultado2.Alocacoes.Where(a => a.ItemIndice == pin.ItemIndice).Sum(a => a.VolumeM3);
    Console.WriteLine($"  volume do item pinado reotimizado na 2a rodada: {volumeReotimizadoDoItemPinado:N2} m3 (esperado <= volume original menos o que foi pinado)");

    foreach (var a in resultado2.Alocacoes)
        Console.WriteLine($"  alocado: item={a.ItemIndice} centro={a.CentroId} semana={capacidadeAjustada.Semanas[a.IndiceSemana]} volume={a.VolumeM3:N2}");
}
