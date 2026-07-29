using Arauco.Otimizador.Common.Domain.Enums;
using Arauco.Otimizador.Common.Domain.Models.Flow;
using Arauco.Otimizador.Data.Dynamo;
using Arauco.Otimizador.WebApi.Flow.Models;
using System.Data;
using Techer.Common.Domain.Repositories;

namespace Arauco.Otimizador.WebApi.Flow.Flow.Refeicao;

public class StepRefeicoes : FlowBase
{
    public override bool IsMatch(DataExchangeRequest model)
    {
        return model.Screen == "STEP_REFEICOES" && model.Action == "data_exchange";
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
                data.RefeicoesSelecionadas = new List<TipoRefeicaoEnum>();

                foreach (var d in model.Data.refeicoes)
                {
                    var id = Convert.ToInt32(d.ToString());
                    if (Enum.IsDefined(typeof(TipoRefeicaoEnum), id))
                    {
                        data.RefeicoesSelecionadas.Add(Enum.ToObject(typeof(TipoRefeicaoEnum), id));
                    }
                }

                if (data.RefeicoesSelecionadas.Count == 0)
                    throw new Exception("Refeição inválida");

                data.CafeManha = null;
                data.Almoco = null;
                data.Janta = null;
                data.CafeNoturno = null;
            }
            catch (Exception)
            {
                throw new Exception("Tipos inválidos");
            }

            await flowRepository.SaveAsync(model.FlowToken, data);

            res = await ProximoPassoAsync(model.Screen, data, keyValueRepository);
        }
        catch (Exception ex)
        {
            return new DataExchangeResponse
            {
                Screen = "STEP_REFEICOES",
                Data = new
                {
                    error_message = ex.Message
                }
            };
        }

        return res;
    }

    public static async Task<DataExchangeResponse> ObterAsync(RefeicaoFlowModel model, IKeyValueRepository keyValueRepository)
    {
        var restaurantesData = await keyValueRepository.GetAsync<List<RestauranteModel>>("RESTAURANTES", false);

        if (restaurantesData.Data == null)
            throw new Exception("Restaurantes não disponíveis");

        var res = new DataExchangeResponse
        {
            Screen = "STEP_REFEICOES",
            Data = new
            {
                tem_cafe_manha = restaurantesData.Data?.Any(r => r.CafeManhaAceitaConsumoLocal || r.CafeManhaAceitaRetirada),
                tem_almoco = restaurantesData.Data?.Any(r => r.AlmocoAceitaConsumoLocal || r.AlmocoAceitaEntrega || r.AlmocoAceitaRetirada),
                tem_janta = restaurantesData.Data?.Any(r => r.JantaAceitaConsumoLocal || r.JantaAceitaEntrega || r.JantaAceitaRetirada),
                tem_cafe_noturno = model.Turno != TurnoEnum.Manha && restaurantesData.Data != null && restaurantesData.Data.Any(r => r.CafeNoturnoAceitaRetirada)
            }
        };

        return res;
    }

    public static async Task<DataExchangeResponse> ProximoPassoAsync(string currentScreen, RefeicaoFlowModel data, IKeyValueRepository keyValueRepository)
    {
        var restaurantesData = await keyValueRepository.GetAsync<List<RestauranteModel>>("RESTAURANTES", false);

        DataExchangeResponse? res = null;
        if (data.RefeicoesSelecionadas.Contains(TipoRefeicaoEnum.CafeManha) && (new string[] { "STEP_REFEICOES" }).Contains(currentScreen))
        {
            res = new DataExchangeResponse()
            {
                Screen = "STEP_CAFE_MANHA",
                Data = new
                {
                    aceita_retirada = restaurantesData.Data?.Any(r => r.CafeManhaAceitaRetirada),
                    aceita_consumo_local = restaurantesData.Data?.Any(r => r.CafeManhaAceitaConsumoLocal),
                    aceita_entrega = restaurantesData.Data?.Any(r => r.CafeManhaAceitaEntrega),
                    extras_estendido = data.ExtrasEstendido
                }
            };
        }
        else if (data.RefeicoesSelecionadas.Contains(TipoRefeicaoEnum.Almoco) && (new string[] { "STEP_REFEICOES", "STEP_CAFE_MANHA" }).Contains(currentScreen))
        {
            res = new DataExchangeResponse()
            {
                Screen = "STEP_ALMOCO",
                Data = new
                {
                    aceita_retirada = restaurantesData.Data?.Any(r => r.AlmocoAceitaRetirada),
                    aceita_consumo_local = restaurantesData.Data?.Any(r => r.AlmocoAceitaConsumoLocal),
                    aceita_entrega = restaurantesData.Data?.Any(r => r.AlmocoAceitaEntrega),
                    extras_estendido = data.ExtrasEstendido
                }
            };
        }
        else if (data.RefeicoesSelecionadas.Contains(TipoRefeicaoEnum.Janta) && (new string[] { "STEP_REFEICOES", "STEP_CAFE_MANHA", "STEP_ALMOCO" }).Contains(currentScreen))
        {
            res = new DataExchangeResponse()
            {
                Screen = "STEP_JANTA",
                Data = new
                {
                    aceita_retirada = restaurantesData.Data?.Any(r => r.JantaAceitaRetirada),
                    aceita_consumo_local = restaurantesData.Data?.Any(r => r.JantaAceitaConsumoLocal),
                    aceita_entrega = restaurantesData.Data?.Any(r => r.JantaAceitaEntrega),
                    extras_estendido = data.ExtrasEstendido
                }
            };
        }
        else if (data.RefeicoesSelecionadas.Contains(TipoRefeicaoEnum.CafeNoturno) && (new string[] { "STEP_REFEICOES", "STEP_CAFE_MANHA", "STEP_ALMOCO", "STEP_JANTA" }).Contains(currentScreen))
        {
            res = new DataExchangeResponse()
            {
                Screen = "STEP_CAFE_NOTURNO",
                Data = new
                {
                    aceita_retirada = restaurantesData.Data?.Any(r => r.CafeNoturnoAceitaRetirada),
                    aceita_consumo_local = restaurantesData.Data?.Any(r => r.CafeNoturnoAceitaConsumoLocal),
                    aceita_entrega = restaurantesData.Data?.Any(r => r.CafeNoturnoAceitaEntrega),
                    extras_estendido = data.ExtrasEstendido
                }
            };
        }
        else
        {
            var qtdeFuncionarios = data.SelecaoIndividual ? data.FuncionariosSelecionados.Count : data.Funcionarios.Count;

            res = new DataExchangeResponse()
            {
                Screen = "STEP_RESUMO",
                Data = new
                {
                    datas = string.Join(", ", data.DatasSelecionadas.Select(d => d.ToString("dd/MM/yyyy"))),
                    qtde = qtdeFuncionarios.ToString(),

                    tem_cafe_manha = data.CafeManha != null,
                    cafe_manha = data.CafeManha?.ObterResumo(qtdeFuncionarios),

                    tem_almoco = data.Almoco != null,
                    almoco = data.Almoco?.ObterResumo(qtdeFuncionarios),

                    tem_janta = data.Janta != null,
                    janta = data.Janta?.ObterResumo(qtdeFuncionarios),

                    tem_cafe_noturno = data.CafeNoturno != null,
                    cafe_noturno = data.CafeNoturno?.ObterResumo(qtdeFuncionarios)
                }
            };
        }

        if (res == null)
            throw new Exception("Nenhuma refeição disponível.");

        return res;
    }
}
