using Arauco.Otimizador.Common.Domain.Enums;
using Arauco.Otimizador.Common.Domain.Models.Flow;
using Arauco.Otimizador.Common.Domain.Models.SolicitacaoRefeicao;
using Arauco.Otimizador.Data.Dynamo;
using Arauco.Otimizador.WebApi.Flow.Models;
using Techer.Common.Domain.Repositories;

namespace Arauco.Otimizador.WebApi.Flow.Flow.Refeicao;

public class StepAlmoco : FlowBase
{
    public override bool IsMatch(DataExchangeRequest model)
    {
        return model.Screen == "STEP_ALMOCO" && model.Action == "data_exchange";
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
                Screen = "STEP_ALMOCO",
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
            var tipoConsumo = (TipoConsumoEnum)Enum.ToObject(typeof(TipoConsumoEnum), Convert.ToInt32(model.Data.modalidade.ToString()));

            string? restauranteId = null;
            int? gelo = null;
            string? localTrabalhoId = null;
            string? talhao = null;
            string? nomeRestaurante = null;
            string? nomeLocalTrabalho = null;

            TimeOnly horario;
            switch (tipoConsumo)
            {
                case TipoConsumoEnum.Retirada:
                case TipoConsumoEnum.Local:
                    {
                        var restaurantesData = await keyValueRepository.GetAsync<List<RestauranteModel>>("RESTAURANTES", false);

                        if (restaurantesData.Data == null)
                            throw new Exception("Restaurantes indisponíveis");

                        var horarioTxt = model.Data.horario.ToString();
                        horario = TimeOnly.ParseExact(horarioTxt, "HHmm");

                        restauranteId = model.Data.restaurante.ToString();
                        var restaurante = restaurantesData.Data.First(r => r.Id == restauranteId);
                        nomeRestaurante = restaurante.Nome;

                        if (restaurante.TemGelo && model.Data.gelo != null)
                        {
                            gelo = Convert.ToInt32(model.Data.gelo.ToString());
                        }
                    }
                    break;

                case TipoConsumoEnum.EntregueEmCampo:
                    {
                        var fazendasData = await keyValueRepository.GetAsync<List<FazendaModel>>("FAZENDAS", false);

                        if (fazendasData.Data == null)
                            throw new Exception("Fazendas indisponíveis");

                        localTrabalhoId = model.Data.fazenda.ToString();
                        nomeLocalTrabalho = fazendasData.Data.First(f => f.Id == localTrabalhoId).Nome;
                        horario = new TimeOnly(7, 00);
                        talhao = model.Data.talhao.ToString();
                    }
                    break;

                default:
                    throw new Exception("Modalidade inválida");
            }

            data.Almoco = new RefeicaoModel
            {
                TipoProdutoEnum = TipoProdutoEnum.Almoco,
                TipoConsumoEnum = tipoConsumo,
                RestauranteId = restauranteId,
                NomeRestaurante = nomeRestaurante,
                Gelo = gelo,
                Extras = extras,
                Horario = horario,
                LocalTrabalhoId = localTrabalhoId,
                NomeLocalTrabalho = nomeLocalTrabalho,
                Talhao = talhao
            };

            data.Janta = null;
            data.CafeNoturno = null;

            await flowRepository.SaveAsync(model.FlowToken, data);
        }
        catch (Exception ex)
        {
            throw new Exception("Ocorreu um erro de validação");
        }

        return await StepRefeicoes.ProximoPassoAsync(model.Screen, data, keyValueRepository);
    }

    private async Task<DataExchangeResponse> AtualizarComponentesAsync(DataExchangeRequest model, IKeyValueRepository keyValueRepository)
    {
        DataExchangeResponse res;
        switch (model.Data.component.ToString())
        {
            case "modalidade":
                if (model.Data.modalidade == null)
                    throw new Exception("Modalidade inválida");
                
                res = await AtualizarModalidadeAsync(model, keyValueRepository);
                break;

            case "municipio":
                if (model.Data.municipioId == null)
                    throw new Exception("Município inválido");

                res = await AtualizarMunicipioAsync(model, keyValueRepository);
                break;

            default:
                throw new Exception("Modalidade inválida");
        }

        return res;
    }

    private async Task<DataExchangeResponse> AtualizarModalidadeAsync(DataExchangeRequest model, IKeyValueRepository keyValueRepository)
    {
        var tipoConsumo = Enum.ToObject(typeof(TipoConsumoEnum), Convert.ToInt32(model.Data.modalidade.ToString()));

        var horarios = ObterHorarios(tipoConsumo);

        bool extrasEstendido = (model.Data.extras_estendido != null && Convert.ToBoolean(model.Data.extras_estendido.ToString()));
        bool temGelo = (model.Data.tem_gelo != null && model.Data.nome_gelo != null && Convert.ToBoolean(model.Data.tem_gelo.ToString()));
        string nomeGelo = temGelo ? model.Data.nome_gelo.ToString() : "Gelo não disponível";

        DataExchangeResponse res;
        switch (tipoConsumo)
        {
            case TipoConsumoEnum.Retirada:
                {
                    var restaurantesData = await keyValueRepository.GetAsync<List<RestauranteModel>>("RESTAURANTES", false);

                    if (restaurantesData.Data == null)
                        throw new Exception("Restaurantes indisponíveis");

                    var restaurantes = restaurantesData.Data;

                    res = new DataExchangeResponse
                    {
                        Screen = "STEP_ALMOCO",
                        Data = new
                        {
                            tem_gelo = temGelo,
                            nome_gelo = nomeGelo,
                            horarios = horarios,
                            extras_estendido = extrasEstendido,
                            restaurantes = restaurantes
                                .Where(r => r.AlmocoAceitaRetirada)
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
                break;

            case TipoConsumoEnum.Local:
                {
                    var restaurantesData = await keyValueRepository.GetAsync<List<RestauranteModel>>("RESTAURANTES", false);

                    if (restaurantesData.Data == null)
                        throw new Exception("Restaurantes indisponíveis");

                    var restaurantes = restaurantesData.Data;

                    res = new DataExchangeResponse
                    {
                        Screen = "STEP_ALMOCO",
                        Data = new
                        {
                            tem_gelo = temGelo,
                            nome_gelo = nomeGelo,
                            horarios = horarios,
                            extras_estendido = extrasEstendido,
                            restaurantes = restaurantes
                                .Where(r => r.AlmocoAceitaConsumoLocal)
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
                break;

            case TipoConsumoEnum.EntregueEmCampo:
                {
                    var fazendasData = await keyValueRepository.GetAsync<List<FazendaModel>>("FAZENDAS", false);

                    if (fazendasData.Data == null)
                        throw new Exception("Fazendas indisponíveis");

                    var municipios = fazendasData
                        .Data
                        .GroupBy(f => new { f.MunicipioId, f.Municipio, f.Uf })
                        .OrderBy(f => f.Key.Municipio).ThenBy(f => f.Key.Uf)
                        .Select(f => new
                        {
                            id = f.Key.MunicipioId,
                            title = $"{f.Key.Municipio}/{f.Key.Uf}"
                        }).ToList();


                    res = new DataExchangeResponse
                    {
                        Screen = "STEP_ALMOCO",
                        Data = new
                        {
                            extras_estendido = extrasEstendido,
                            municipios,
                            fazendas = new List<object>()
                        }
                    };
                }
                break;

            default:
                throw new Exception("Modalidade inválida");
        }

        return res;
    }

    private async Task<DataExchangeResponse> AtualizarMunicipioAsync(DataExchangeRequest model, IKeyValueRepository keyValueRepository)
    {
        var fazendasData = await keyValueRepository.GetAsync<List<FazendaModel>>("FAZENDAS", false);

        if (fazendasData.Data == null)
            throw new Exception("Fazendas indisponíveis");

        bool extrasEstendido = (model.Data.extras_estendido != null && Convert.ToBoolean(model.Data.extras_estendido.ToString()));

        var municipioId = model.Data.municipioId.ToString();

        var fazendas = fazendasData
            .Data
            .Where(f => f.MunicipioId == municipioId)
            .ToList();

        return new DataExchangeResponse()
        {
            Screen = "STEP_ALMOCO",
            Data = new
            {
                extras_estendido = extrasEstendido,
                fazendas = fazendas
                                .Select(r => new
                                {
                                    id = r.Id,
                                    title = r.Nome
                                }).ToList()
            }
        };
    }

    private List<DataSourceModel> ObterHorarios(TipoConsumoEnum tipoConsumo)
    {
        var horarios = new List<TimeOnly>();

        if (tipoConsumo == TipoConsumoEnum.Retirada)
        {
            horarios.Add(new TimeOnly(5, 30));
            horarios.Add(new TimeOnly(6, 00));
            horarios.Add(new TimeOnly(6, 30));
            horarios.Add(new TimeOnly(7, 00));
        }

        horarios.Add(new TimeOnly(11, 0));
        horarios.Add(new TimeOnly(11, 30));
        horarios.Add(new TimeOnly(12, 0));
        horarios.Add(new TimeOnly(12, 30));
        horarios.Add(new TimeOnly(13, 0));

        return horarios.Select(h => new DataSourceModel
        {
            Id = h.ToString("HHmm"),
            Title = h.ToString("HH:mm")
        }).ToList();
    }
}
