using Arauco.Otimizador.Common.Domain.Enums;
using Arauco.Otimizador.Common.Domain.Models;
using Techer.Common.Domain.DataSource;

namespace Arauco.Otimizador.Common.Domain.Interfaces
{
    public interface ILogRepository
    {
        Task SalvarAsync(LogModel model);
        Task<DdbDatasourceResult<LogModel>> BuscarAsync(TipoLogEnum tipoLog, string agrupador, DynamoDbDataSourceRequest<LogFilterModel> request);
    }
}
