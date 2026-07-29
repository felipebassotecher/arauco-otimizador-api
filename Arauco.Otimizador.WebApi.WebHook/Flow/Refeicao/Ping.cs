using Arauco.Otimizador.Data.Dynamo;
using Arauco.Otimizador.WebApi.Flow.Models;
using Techer.Common.Domain.Repositories;

namespace Arauco.Otimizador.WebApi.Flow.Flow.Refeicao;

public class Ping : FlowBase
{
    public override bool IsMatch(DataExchangeRequest model)
    {
        return model.Action == "ping";
    }

    public override async Task<DataExchangeResponse> RunAsync(DataExchangeRequest model, IFlowRepository flowRepository, IKeyValueRepository keyValueRepository)
    {
        return new DataExchangeResponse
        {
            Data = new
            {
                status = "active"
            }
        };
    }
}
