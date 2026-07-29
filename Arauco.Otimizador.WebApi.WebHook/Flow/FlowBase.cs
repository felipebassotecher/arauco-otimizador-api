using Arauco.Otimizador.Data.Dynamo;
using Arauco.Otimizador.WebApi.Flow.Models;
using Techer.Common.Domain.Repositories;

namespace Arauco.Otimizador.WebApi.Flow.Flow;

public abstract class FlowBase
{
    public abstract bool IsMatch(DataExchangeRequest model);
    public abstract Task<DataExchangeResponse> RunAsync(DataExchangeRequest model, IFlowRepository flowRepository, IKeyValueRepository keyValueRepository);
}
