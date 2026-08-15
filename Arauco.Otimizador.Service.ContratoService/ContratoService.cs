using Arauco.Otimizador.Common.Domain.Enums.Demanda;
using Arauco.Otimizador.Common.Domain.Models.Contrato;
using Arauco.Otimizador.Common.Domain.Services.Contrato;
using Arauco.Otimizador.Common.Gcp;
using Arauco.Otimizador.Data.Entities;
using Arauco.Otimizador.Data.Entities.Contrato;
using Arauco.Otimizador.Service.Base;
using Techer.Common.Domain.Exceptions;
using Techer.Common.Domain.Interfaces;
using Techer.Common.Id;

namespace Arauco.Otimizador.Service.ContratoService;

// Enriquece um cenário com dados de contrato vindos do GCP (BigQuery/Dataform). Hoje faz duas
// consultas — uma para o cabeçalho do contrato (cliente + tipo de frete) e outra para os itens
// (produto + volume) —, porque são duas queries .sqlx separadas no Dataform, cada uma num formato/
// tabela diferente; o vínculo entre as duas é feito aqui, por ClienteId.
public class ContratoService : ServiceBase, IContratoService
{
    // Nomes das tabelas/views que o Dataform (.sqlx) materializa no BigQuery — placeholders. Troque
    // pelos nomes reais dos datasets/tabelas em appsettings.json antes de usar:
    //   "Gcp": {
    //     "BigQuery": {
    //       "TabelaContratosCabecalho": "SEU_PROJETO.SEU_DATASET.TABELA_CONTRATOS_CABECALHO",
    //       "TabelaContratosItens": "SEU_PROJETO.SEU_DATASET.TABELA_CONTRATOS_ITENS"
    //     }
    //   }
    private const string TabelaContratosCabecalhoConfigKey = "Gcp:BigQuery:TabelaContratosCabecalho";
    private const string TabelaContratosItensConfigKey = "Gcp:BigQuery:TabelaContratosItens";

    public ContratoService(IUnitOfWork unitOfWork, IEnvironmentVariables environmentVariables)
        : base(unitOfWork, environmentVariables)
    {
    }

    public async Task<List<ContratoResponse>> EnriquecerAsync(string cenarioId)
    {
        if (!await unitOfWork.CenarioRepository.AnyAsync(c => c.CenarioId == cenarioId))
            throw new NotFoundException("Cenário não encontrado");

        var cabecalhos = await ConsultarCabecalhosAsync();
        var itensBrutos = await ConsultarItensAsync();

        var itensBrutosPorCliente = itensBrutos
            .GroupBy(i => i.ClienteId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var respostas = new List<ContratoResponse>();

        foreach (var contrato in cabecalhos)
        {
            var itensBrutosDoCliente = itensBrutosPorCliente.GetValueOrDefault(contrato.ClienteId, []);

            var itens = itensBrutosDoCliente
                .Select(i => new ContratoItem
                {
                    ContratoItemId = IdGenerator.NewSync(12),
                    ContratoId = contrato.ContratoId,
                    ProdutoId = i.ProdutoId,
                    ProdutoNome = i.ProdutoNome,
                    Volume = i.Volume,
                })
                .ToList();

            respostas.Add(MapearResponse(contrato, itens));
        }

        return respostas;
    }

    // Query 1 do Dataform (.sqlx) — cabeçalho do contrato (cliente + tipo de frete). Os aliases
    // (AS ClienteId, etc.) abaixo isolam o resto do código do nome real das colunas produzidas pelo
    // .sqlx — ajuste só o SELECT se os nomes reais forem diferentes.
    private async Task<List<Contrato>> ConsultarCabecalhosAsync()
    {
        var tabela = ObterNomeTabela(TabelaContratosCabecalhoConfigKey);

        var sql = $"""
            SELECT
                cliente_id AS ClienteId,
                cliente_nome AS ClienteNome,
                tipo_frete AS TipoFrete
            FROM `{tabela}`
            """;

        var resultado = await BigQueryHelper.ExecutarConsultaAsync(environmentVariables, sql);

        var contratos = new List<Contrato>();
        foreach (var linha in resultado)
        {
            contratos.Add(new Contrato
            {
                ContratoId = IdGenerator.NewSync(),
                ClienteId = linha["ClienteId"]?.ToString() ?? "",
                ClienteNome = linha["ClienteNome"]?.ToString() ?? "",
                TipoFreteEnum = ConverterTipoFrete(linha["TipoFrete"]?.ToString()),
            });
        }

        return contratos;
    }

    // Query 2 do Dataform (.sqlx) — itens do contrato (produto + volume), num formato/tabela
    // diferente da query de cabeçalho. `ClienteId` aqui é só o vínculo com o cabeçalho — o item
    // final (ContratoItem, com ContratoId) só é montado depois de casar os dois resultados.
    private async Task<List<ItemBruto>> ConsultarItensAsync()
    {
        var tabela = ObterNomeTabela(TabelaContratosItensConfigKey);

        var sql = $"""
            SELECT
                cliente_id AS ClienteId,
                produto_id AS ProdutoId,
                produto_nome AS ProdutoNome,
                volume AS Volume
            FROM `{tabela}`
            """;

        var resultado = await BigQueryHelper.ExecutarConsultaAsync(environmentVariables, sql);

        var itens = new List<ItemBruto>();
        foreach (var linha in resultado)
        {
            itens.Add(new ItemBruto(
                linha["ClienteId"]?.ToString() ?? "",
                linha["ProdutoId"]?.ToString() ?? "",
                linha["ProdutoNome"]?.ToString() ?? "",
                linha["Volume"] is null ? 0 : Convert.ToDecimal(linha["Volume"])));
        }

        return itens;
    }

    private string ObterNomeTabela(string configKey)
    {
        var tabela = environmentVariables[configKey];

        if (string.IsNullOrWhiteSpace(tabela))
            throw new InvalidOperationException(
                $"Configuração '{configKey}' ausente. Configure-a em appsettings.json (hoje é só um " +
                "placeholder) com o nome real da tabela/dataset do BigQuery antes de usar esta integração.");

        return tabela;
    }

    private static TipoFreteEnum ConverterTipoFrete(string? valor) =>
        string.Equals(valor?.Trim(), "CIF", StringComparison.OrdinalIgnoreCase)
            ? TipoFreteEnum.CIF
            : TipoFreteEnum.FOB;

    private static ContratoResponse MapearResponse(Contrato contrato, List<ContratoItem> itens)
    {
        return new ContratoResponse
        {
            ClienteId = contrato.ClienteId,
            ClienteNome = contrato.ClienteNome,
            TipoFrete = contrato.TipoFreteEnum.ToString(),
            Itens = itens.Select(i => new ContratoItemResponse
            {
                ProdutoId = i.ProdutoId,
                ProdutoNome = i.ProdutoNome,
                Volume = i.Volume,
            }).ToList(),
        };
    }

    // Linha bruta da query de itens, antes de saber a qual Contrato (ContratoId sintético) ela
    // pertence — o vínculo real (ContratoItem.ContratoId) só existe depois do agrupamento por
    // ClienteId em EnriquecerAsync.
    private sealed record ItemBruto(string ClienteId, string ProdutoId, string ProdutoNome, decimal Volume);
}
