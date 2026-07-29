using Arauco.Otimizador.Common.Domain.Enums;
using Arauco.Otimizador.Common.Domain.Models.Flow;
using Arauco.Otimizador.Common.Domain.Models.SolicitacaoRefeicao;
using Arauco.Otimizador.Data.Dynamo;
using Arauco.Otimizador.WebApi.Flow.Models;
using Techer.Common.Domain.Repositories;

namespace Arauco.Otimizador.WebApi.Flow.Flow.Refeicao;

public class StepCafeNoturno : FlowBase
{
    public override bool IsMatch(DataExchangeRequest model)
    {
        return model.Screen == "STEP_CAFE_NOTURNO" && model.Action == "data_exchange";
    }

    public override async Task<DataExchangeResponse> RunAsync(DataExchangeRequest model, IFlowRepository flowRepository, IKeyValueRepository keyValueRepository)
    {
        DataExchangeResponse res;
        try
        {
            if (model.Data.component != null)
            {
                res = await AtualizarComponentesAsync(model, keyValueRepository);
            }
            else
            {
                res = await FinalizarAsync(model, flowRepository, keyValueRepository);
            }
        }
        catch (Exception ex)
        {
            return new DataExchangeResponse
            {
                Screen = "STEP_CAFE_NOTURNO",
                Data = new
                {
                    error_message = ex.Message
                }
            };
        }

        return res;
    }

    private async Task<DataExchangeResponse> FinalizarAsync(DataExchangeRequest model, IFlowRepository flowRepository, IKeyValueRepository keyValueRepository)
    {
        var data = await flowRepository.GetAsync<RefeicaoFlowModel>(model.FlowToken, true);

        if (data == null)
            throw new Exception("Formulário inválido.");

        var restaurantesData = await keyValueRepository.GetAsync<List<RestauranteModel>>("RESTAURANTES", false);

        if (restaurantesData.Data == null)
            throw new Exception("Restaurantes indisponíveis");

        // Extras
        int? extras = null;
        try
        {
            if (model.Data.extras2 != null)
            {
                extras = Convert.ToInt32(model.Data.extras2.ToString());
            }
            else
            {
                extras = Convert.ToInt32(model.Data.extras.ToString());
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Ocorreu um erro de validação das refeições extras.");
        }

        if (extras.HasValue && (extras < 0 || extras > 200))
            throw new Exception("Refeições extras inválida.");

        // Parse
        try
        {
            var horarioTxt = model.Data.horario.ToString();
            var horario = TimeOnly.ParseExact(horarioTxt, "HHmm");

            var tipoConsumo = (TipoConsumoEnum)Enum.ToObject(typeof(TipoConsumoEnum), Convert.ToInt32(model.Data.modalidade.ToString()));

            var restauranteId = model.Data.restaurante.ToString();
            var restaurante = restaurantesData.Data.First(r => r.Id == restauranteId);
            
            int? gelo = null;
            if (tipoConsumo != TipoConsumoEnum.EntregueEmCampo && restaurante.TemGelo && model.Data.gelo != null)
            {
                gelo = Convert.ToInt32(model.Data.gelo.ToString());
            }

            data.CafeNoturno = new RefeicaoModel
            {
                TipoProdutoEnum = TipoProdutoEnum.CafeNoturno,
                TipoConsumoEnum = tipoConsumo,
                RestauranteId = restauranteId,
                NomeRestaurante = restaurante.Nome,
                Gelo = gelo,
                Extras = extras,
                Horario = horario
            };
        }
        catch (Exception ex)
        {
            throw new Exception("Ocorreu um erro de validação");
        }

        await flowRepository.SaveAsync(model.FlowToken, data);

        return await StepRefeicoes.ProximoPassoAsync(model.Screen, data, keyValueRepository);
    }

    private async Task<DataExchangeResponse> AtualizarComponentesAsync(DataExchangeRequest model, IKeyValueRepository keyValueRepository)
    {
        if (model.Data.component != "modalidade" || model.Data.modalidade == null)
            throw new Exception("Modalidade inválida");

        var restaurantesData = await keyValueRepository.GetAsync<List<RestauranteModel>>("RESTAURANTES", false);

        if (restaurantesData.Data == null)
            throw new Exception("Restaurantes indisponíveis");

        var restaurantes = restaurantesData.Data;
        var tipoConsumo = Enum.ToObject(typeof(TipoConsumoEnum), Convert.ToInt32(model.Data.modalidade.ToString()));

        bool extrasEstendido = (model.Data.extras_estendido != null && Convert.ToBoolean(model.Data.extras_estendido.ToString()));
        bool temGelo = (model.Data.tem_gelo != null && model.Data.nome_gelo != null && Convert.ToBoolean(model.Data.tem_gelo.ToString()));
        string nomeGelo = temGelo ? model.Data.nome_gelo.ToString() : "Gelo não disponível";

        switch (tipoConsumo)
        {
            case TipoConsumoEnum.Retirada:
                restaurantes = restaurantes
                    .Where(r => r.CafeNoturnoAceitaRetirada)
                    .ToList();
                break;

            case TipoConsumoEnum.Local:
                restaurantes = restaurantes
                    .Where(r => r.CafeNoturnoAceitaConsumoLocal)
                    .ToList();
                break;

            default:
                throw new Exception("Modalidade inválida");
        }

        return new DataExchangeResponse()
        {
            Screen = "STEP_CAFE_NOTURNO",
            Data = new
            {
                tem_gelo = temGelo,
                nome_gelo = nomeGelo,
                extras_estendido = extrasEstendido,
                restaurantes = restaurantes
                                .Select(r => new DataSourceModel
                                {
                                    Id = r.Id,
                                    Title = r.Nome,
                                    Description = $"{r.Municipio}/{r.Uf}",
                                    OnSelectAction = new
                                    {
                                        name = "update_data",
                                        payload = new
                                        {
                                            tem_gelo = r.TemGelo,
                                            nome_gelo = r.TemGelo ? r.NomeGelo : "Gelo não disponível"
                                        }
                                    }
                                }).ToList()
            }
        };
    }

}
