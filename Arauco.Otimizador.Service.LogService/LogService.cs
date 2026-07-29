using Arauco.Otimizador.Common.Domain.Enums;
using Arauco.Otimizador.Common.Domain.Interfaces;
using Arauco.Otimizador.Common.Domain.Models;
using Arauco.Otimizador.Common.Domain.Services;
using Arauco.Otimizador.Common.Domain.Session;
using Techer.Common.Domain.DataSource;
using Techer.Common.Id;

namespace Arauco.Otimizador.Service.LogService;

public class LogService : ILogService
{
    private readonly ILogRepository logRepository;

    public LogService(ILogRepository logRepository)
    {
        this.logRepository = logRepository;
    }

    public async Task<DdbDatasourceResult<LogModel>> BuscarAsync(TipoLogEnum tipoLogEnum, string chave, DynamoDbDataSourceRequest<LogFilterModel> request)
    {
        return await this.logRepository.BuscarAsync(tipoLogEnum, chave, request);
    }

    public async Task NovoAsync(TipoLogEnum tipo, int agrupador, string descricao, BaseSessionModel session)
    {
        await NovoAsync(tipo, agrupador.ToString(), descricao, session);
    }

    public async Task NovoAsync(TipoLogEnum tipo, string agrupador, string descricao, BaseSessionModel session)
    {
        if (string.IsNullOrWhiteSpace(descricao))
            return;

        descricao = descricao.Trim();

        if (descricao.Length > 5000)
            descricao = descricao.Trim()[..5000];

        var log = new LogModel
        {
            LogId = IdGenerator.NewOrdered(),
            Agrupador = agrupador,
            TipoLogEnum = tipo,
            Descricao = descricao,
            DataHora = DateTime.UtcNow                
        };

        await this.logRepository.SalvarAsync(log);
    }
}