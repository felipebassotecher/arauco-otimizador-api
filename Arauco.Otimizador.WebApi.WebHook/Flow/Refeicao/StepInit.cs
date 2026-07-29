using Arauco.Otimizador.Common.Domain.Models.Flow;
using Arauco.Otimizador.Data.Dynamo;
using Arauco.Otimizador.WebApi.Flow.Models;
using Techer.Common.Domain.Repositories;

namespace Arauco.Otimizador.WebApi.Flow.Flow.Refeicao;

public class StepInit : FlowBase
{
    public override bool IsMatch(DataExchangeRequest model)
    {
        return model.Action == "INIT";
    }

    public override async Task<DataExchangeResponse> RunAsync(DataExchangeRequest model, IFlowRepository flowRepository, IKeyValueRepository keyValueRepository)
    {
        DataExchangeResponse res;
        try
        {
            var data = await flowRepository.GetAsync<RefeicaoFlowModel>(model.FlowToken, true);

            if (data == null)
                throw new Exception("Formulário inválido.");

            var isValid = (data.DataHoraExpiracao > DateTime.UtcNow);

            res = new DataExchangeResponse
            {
                Screen = "STEP_DATA_TURNO",
                Data = new
                {
                    is_valid = isValid,
                    datas = data.Datas.Select(d => new
                    {
                        id = d.ToString("yyyyMMdd"),
                        title = d.ToString("dd/MM/yyyy"),
                        description = ParseDiaSemana(d.DayOfWeek)
                    })
                }
            };

        }
        catch (Exception ex)
        {
            res = new DataExchangeResponse
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

    private string ParseDiaSemana(DayOfWeek dayOfWeek)
    {
        return dayOfWeek switch
        {
            DayOfWeek.Sunday => "Domingo",
            DayOfWeek.Monday => "Segunda-feira",
            DayOfWeek.Tuesday => "Terça-feira",
            DayOfWeek.Wednesday => "Quarta-feira",
            DayOfWeek.Thursday => "Quinta-feira",
            DayOfWeek.Friday => "Sexta-feira",
            DayOfWeek.Saturday => "Sábado",
            _ => string.Empty,
        };
    }
}
