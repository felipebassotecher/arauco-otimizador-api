using Arauco.Otimizador.Common.Domain.Models.Flow;
using Arauco.Otimizador.Data.Dynamo;
using Arauco.Otimizador.WebApi.Flow.Models;
using Techer.Common.Domain.Repositories;

namespace Arauco.Otimizador.WebApi.Flow.Flow.Refeicao;

public class StepColabs : FlowBase
{
    public static async Task<DataExchangeResponse> ObterAsync(RefeicaoFlowModel data)
    {
        return new DataExchangeResponse
        {
            Screen = "STEP_COLABS",
            Data = new
            {
                colabs = data.Funcionarios.OrderBy(f => f.Value).Select(f => new
                {
                    id = f.Key,
                    title = f.Value
                })
            }
        };
    }

    public override bool IsMatch(DataExchangeRequest model)
    {
        return model.Screen == "STEP_COLABS" && model.Action == "data_exchange";
    }

    public override async Task<DataExchangeResponse> RunAsync(DataExchangeRequest model, IFlowRepository flowRepository, IKeyValueRepository keyValueRepository)
    {
        DataExchangeResponse res;
        try
        {
            var data = await flowRepository.GetAsync<RefeicaoFlowModel>(model.FlowToken, true);

            if (data == null)
                throw new Exception("Formulário inválido.");

            // Parse
            try
            {
                data.FuncionariosSelecionados = new List<string>();

                foreach (var d in model.Data.colabs)
                {
                    data.FuncionariosSelecionados.Add(d.ToString());
                }
            }
            catch (Exception)
            {
                throw new Exception("Datas inválidas");
            }

            await flowRepository.SaveAsync(model.FlowToken, data);

            res = await StepRefeicoes.ObterAsync(data, keyValueRepository);
        }
        catch (Exception ex)
        {
            return new DataExchangeResponse
            {
                Screen = "STEP_DATA_TURNO",
                Data = new
                {
                    error_message = ex.Message
                }
            };
        }

        return res;
    }
}
