using Amazon.DynamoDBv2.DocumentModel;
using Arauco.Otimizador.Common.Domain.Enums;
using Arauco.Otimizador.Common.Domain.Interfaces;
using Arauco.Otimizador.Common.Domain.Models;
using Techer.Aws.Dynamo;
using Techer.Common.Domain.DataSource;
using Techer.Common.Domain.Exceptions;

namespace Arauco.Otimizador.Data.Dynamo
{
    public class LogRepository : GenericRepository<Docs.LogDoc>, ILogRepository
    {
        public async Task SalvarAsync(LogModel model)
        {
            await UpdateItemAsync(Docs.LogDoc.FromModel(model));
        }

        public async Task<DdbDatasourceResult<LogModel>> BuscarAsync(TipoLogEnum tipoLog, string agrupador, DynamoDbDataSourceRequest<LogFilterModel> request)
        {
            var dataInicial = DateTime.UtcNow;
            DateTime? dataFinal = null;

            if (request.Filters == null)
                throw new ArgumentException("Os filtros são obrigatórios.");

            if (!string.IsNullOrEmpty(request.Filters.Pt))
            {
                switch (request.Filters.Pt)
                {
                    case "1H":
                        dataInicial = dataInicial.AddHours(-1);
                        break;

                    case "1D":
                        dataInicial = dataInicial.AddDays(-1);
                        break;

                    case "1W":
                        dataInicial = dataInicial.AddDays(-7);
                        break;

                    default:
                        throw new InvalidOperationException("Filtro inválido.");
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(request.Filters.Inicio) && DateTime.TryParse(request.Filters.Inicio, out DateTime tmpInicial))
                    dataInicial = tmpInicial;
                if (!string.IsNullOrEmpty(request.Filters.Fim) && DateTime.TryParse(request.Filters.Fim, out DateTime tmpFinal))
                    dataFinal = tmpFinal;
            }

            if (dataFinal.HasValue == false)
            {
                dataFinal = DateTime.UtcNow;
            }
            if (dataInicial > dataFinal)
                throw new ApiException("Intervalo de datas inválido.");

            var startSort = $"{dataInicial:s}";
            var endSort = $"{dataFinal:s}";

            var config = new QueryOperationConfig
            {
                KeyExpression = new Expression
                {
                    ExpressionStatement = "#pk = :v_partitionKey and #sk between :v_startSort and :v_endSort",
                    ExpressionAttributeNames = {
                        { "#pk", "PK" },
                        { "#sk", "Sort" }
                    },
                    ExpressionAttributeValues = new Dictionary<string, DynamoDBEntry>()
                    {
                        {":v_partitionKey", $"{(int)tipoLog}#{agrupador}"},
                        {":v_startSort", startSort},
                        {":v_endSort", endSort }
                    },
                },
                ConsistentRead = false,
                Limit = request.Take,
                PaginationToken = request.AfterToken
            };

            if (!string.IsNullOrEmpty(request.BeforeToken))
            {
                config.BackwardSearch = true;
                config.PaginationToken = request.BeforeToken;
            }

            var docs = await GetNextPaginatedAsync(config, new string[] { "PK", "Sort" }, true);

            var data = docs
                .Data
                .Select(d => d.ToModel())
                .OrderByDescending(d => d.DataHora)
                .ToList();

            return new DdbDatasourceResult<LogModel>()
            {
                Data = data,
                Cursor = new DdbDataSourceCursor
                {
                    AfterToken = docs.Cursor.AfterToken,
                    BeforeToken = docs.Cursor.BeforeToken
                }
            };
        }
    }
}
