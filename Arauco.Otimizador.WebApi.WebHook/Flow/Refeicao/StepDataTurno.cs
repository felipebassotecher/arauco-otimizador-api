using Arauco.Otimizador.Common.Domain.Enums;
using Arauco.Otimizador.Common.Domain.Models.Flow;
using Arauco.Otimizador.Data.Dynamo;
using Arauco.Otimizador.WebApi.Flow.Models;
using Techer.Common.Domain.Repositories;

namespace Arauco.Otimizador.WebApi.Flow.Flow.Refeicao;

public class StepDataTurno : FlowBase
{
    public override bool IsMatch(DataExchangeRequest model)
    {
        return model.Screen == "STEP_DATA_TURNO" && model.Action == "data_exchange";
    }

    public override async Task<DataExchangeResponse> RunAsync(DataExchangeRequest model, IFlowRepository flowRepository, IKeyValueRepository keyValueRepository)
    {
        DataExchangeResponse res;
        try
        {
            var data = await flowRepository.GetAsync<RefeicaoFlowModel>(model.FlowToken, true);

            if (data == null)
                throw new Exception("Formulário inválido.");

            if (data.DataHoraExpiracao < DateTime.UtcNow)
            {
                res = new DataExchangeResponse
                {
                    Screen = "STEP_DATA_TURNO",
                    Data = new
                    {
                        is_valid = false
                    }
                };
            }
            else
            {
                // Parse
                try
                {
                    data.DatasSelecionadas = new List<DateOnly>();

                    foreach (var d in model.Data.datas)
                    {
                        var dataSelecionada = DateOnly.ParseExact(d.ToString(), "yyyyMMdd");

                        //if (dataSelecionada == dataAtual)
                        //{
                        data.DatasSelecionadas.Add(dataSelecionada);
                        //}
                    }

                    data.Turno = (TurnoEnum)Enum.ToObject(typeof(TurnoEnum), Convert.ToInt32(model.Data.turno.ToString()));

                    data.SelecaoIndividual = (model.Data.selecao != null && Convert.ToBoolean(model.Data.selecao) == true);
                }
                catch (Exception)
                {
                    throw new Exception("Datas inválidas");
                }

                if (data.DatasSelecionadas.Count == 0)
                    throw new Exception("Data indisponível");

                await flowRepository.SaveAsync(model.FlowToken, data);

                if (data.SelecaoIndividual)
                {
                    res = await StepColabs.ObterAsync(data);
                }
                else
                {
                    res = await StepRefeicoes.ObterAsync(data,keyValueRepository);
                }
            }
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
