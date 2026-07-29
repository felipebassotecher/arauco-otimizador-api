using Arauco.Otimizador.Common.Domain.Enums;
using Arauco.Otimizador.Common.Domain.Models;
using Arauco.Otimizador.Common.Domain.Session;
using Techer.Common.Domain.DataSource;

namespace Arauco.Otimizador.Common.Domain.Services
{
    public interface ILogService
    {
        Task NovoAsync(TipoLogEnum tipo, int chave, string descricao, BaseSessionModel session);
        Task NovoAsync(TipoLogEnum tipo, string chave, string descricao, BaseSessionModel session);
        Task<DdbDatasourceResult<LogModel>> BuscarAsync(TipoLogEnum tipoLogEnum, string chave, DynamoDbDataSourceRequest<LogFilterModel> request);
    }
}