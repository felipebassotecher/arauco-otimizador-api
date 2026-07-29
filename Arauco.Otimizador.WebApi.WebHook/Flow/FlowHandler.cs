using Arauco.Otimizador.Data.Dynamo;
using Arauco.Otimizador.WebApi.Flow.Models;
using Techer.Common.Domain.Repositories;

namespace Arauco.Otimizador.WebApi.Flow.Flow;

public abstract class FlowHandler
{
    private readonly List<FlowBase> flows;
    private readonly IFlowRepository flowRepository;
    private readonly IKeyValueRepository keyValueRepository;

    public FlowHandler(List<FlowBase> flows, IFlowRepository flowRepository, IKeyValueRepository keyValueRepository)
    {
        this.flows = flows;
        this.flowRepository = flowRepository;
        this.keyValueRepository = keyValueRepository;
    }

    public async Task<DataExchangeResponse?> HandleAsync(DataExchangeRequest model)
    {
        foreach (var flow in flows)
        {
            if (flow.IsMatch(model))
            {
                return await flow.RunAsync(model, flowRepository, keyValueRepository);
            }
        }

        return null;
    }
}
