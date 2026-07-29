using Arauco.Otimizador.Data.Dynamo;
using Techer.Common.Domain.Repositories;

namespace Arauco.Otimizador.WebApi.Flow.Flow.Refeicao;

public class RefeicaoFlowHandler : FlowHandler
{
    public RefeicaoFlowHandler(IFlowRepository flowRepository, IKeyValueRepository keyValueRepository) : base([
        new Ping(),
        new StepInit(),
        new StepDataTurno(),
        new StepColabs(),
        new StepRefeicoes(),
        new StepCafeManha(),
        new StepAlmoco(),
        new StepJanta(),
        new StepCafeNoturno(),
        new StepResumo(),
        ], flowRepository, keyValueRepository)
    {        
    }
}
