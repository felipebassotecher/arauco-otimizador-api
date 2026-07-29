using Arauco.Otimizador.Common.Domain.Models.Flow;
using Arauco.Otimizador.Data.Dynamo;
using Arauco.Otimizador.WebApi.Flow.Models;
using Techer.Common.Domain.Repositories;

namespace Arauco.Otimizador.WebApi.Flow.Flow.Refeicao;

public class StepResumo : FlowBase
{
    public override bool IsMatch(DataExchangeRequest model)
    {
        return model.Screen == "STEP_RESUMO" && model.Action == "data_exchange";
    }

    public override async Task<DataExchangeResponse> RunAsync(DataExchangeRequest model, IFlowRepository flowRepository, IKeyValueRepository keyValueRepository)
    {
        DataExchangeResponse res;
        try
        {
            var data = await flowRepository.GetAsync<RefeicaoFlowModel>(model.FlowToken, true);

            if (data == null)
                throw new Exception("Formulário inválido.");

            // TODO: Validations

            res = new FinalResponsePayload
            {
                Data = new
                {
                }
            };
        }
        catch (Exception ex)
        {
            return new DataExchangeResponse
            {
                Screen = "STEP_RESUMO",
                Data = new
                {
                    error_message = ex.Message
                }
            };
        }

        return res;
    }
}
