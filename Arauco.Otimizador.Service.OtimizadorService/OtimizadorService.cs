using Arauco.Otimizador.Common.Domain.Models.Otimizador;
using Arauco.Otimizador.Common.Domain.Services.Otimizador;
using Arauco.Otimizador.Data.Entities;
using Arauco.Otimizador.Data.Entities.Cenario;
using Arauco.Otimizador.Data.Entities.Otimizador;
using Arauco.Otimizador.Service.Base;
using Arauco.Otimizador.Service.OtimizadorService.Dados;
using Arauco.Otimizador.Service.OtimizadorService.Mapeamento;
using Arauco.Otimizador.Service.OtimizadorService.Modelo;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using Techer.Common.Domain.Exceptions;
using Techer.Common.Domain.Interfaces;
using Techer.Common.Id;

namespace Arauco.Otimizador.Service.OtimizadorService;

public class OtimizadorService : ServiceBase, IOtimizadorService
{
    private readonly Executor _executor = new();

    public OtimizadorService(
        IUnitOfWork unitOfWork,
        IEnvironmentVariables environmentVariables) : base(unitOfWork, environmentVariables)
    {
    }

    public async Task<OtimizacaoResponse> OtimizarCenarioAsync(string cenarioId, OtimizacaoRequest? request)
    {
        var cenario = await unitOfWork.CenarioRepository
            .FirstOrDefaultAsync(c => c.CenarioId == cenarioId)
            ?? throw new NotFoundException("Cenário não encontrado");

        var demandas = await unitOfWork.DemandaRepository
            .Where(d => d.CenarioId == cenarioId)
            .ToListAsync();

        if (demandas.Count == 0)
            throw new ApiException("Cenário sem demandas carregadas");

        var pastaDatasets = ObterCaminhoDatasets();
        var carregador = await _executor.CarregarAsync(pastaDatasets);

        var carteira = DemandaParaCarteiraMapper.Mapear(demandas, carregador.Produtos, carregador.Elegibilidade);
        var dados = carregador.ComCarteira(carteira);

        var config = CriarConfig(request);

        var execucao = _executor.Executar(dados, config);

        var resultadoId = await IdGenerator.New();
        var resultado = new CenarioOtimizacaoResultado
        {
            ResultadoId = resultadoId,
            CenarioId = cenarioId,
            StatusSolver = execucao.Solver.Status,
            Segundos = execucao.Solver.Segundos,
            Objetivo = execucao.Solver.Objetivo,
            Variaveis = execucao.Solver.Variaveis,
            Binarias = execucao.Solver.Binarias,
            GreedyInicialM3 = (decimal)execucao.Solver.GreedyInicialM3,
            GeradoEm = execucao.GeradoEm,
            FatorCapacidade = (decimal)execucao.Resumo.FatorCapacidade,
            CapacidadeTotal = execucao.Resumo.CapacidadeTotal,
            DemandaTotalM3 = (decimal)execucao.Resumo.DemandaTotalM3,
            DemandaElegivelM3 = (decimal)execucao.Resumo.DemandaElegivelM3,
            ExcluidoPreflightM3 = (decimal)execucao.Resumo.ExcluidoPreflightM3,
            AlocadoM3 = (decimal)execucao.Resumo.AlocadoM3,
            NaoAlocadoM3 = (decimal)execucao.Resumo.NaoAlocadoM3,
            PercentualAlocado = (decimal)execucao.Resumo.PercentualAlocado,
            Itens = execucao.Resumo.Itens,
            ItensExcluidos = execucao.Resumo.ItensExcluidos
        };

        unitOfWork.CenarioOtimizacaoResultadoRepository.Add(resultado);

        var alocacoes = new List<OtimizacaoAlocacao>();
        foreach (var a in execucao.Alocacoes)
        {
            var semana = SemanaIso.Parse(a.Semana);
            alocacoes.Add(new OtimizacaoAlocacao
            {
                AlocacaoId = await IdGenerator.New(),
                ResultadoId = resultadoId,
                Cliente = a.ClienteId,
                Produto = a.ProdutoId,
                LinhaProdutoId = a.LinhaProdutoId,
                CentroId = a.CentroId,
                Centro = a.Centro,
                Ano = semana.Ano,
                Semana = semana.Numero,
                VolumeM3 = (decimal)a.VolumeM3,
                Cif = a.Cif,
                Prioridade = a.Prioridade,
                MotivoSemana = a.MotivoSemana,
                MotivoPlanta = a.MotivoPlanta,
                FolgaAntesM3 = (decimal)a.FolgaAntesM3,
                PlantasElegiveis = a.PlantasElegiveis,
                PosicaoPrioridade = a.PosicaoPrioridade
            });
        }
        unitOfWork.OtimizacaoAlocacaoRepository.AddRange(alocacoes);

        var naoAlocados = new List<OtimizacaoNaoAlocado>();
        foreach (var n in execucao.NaoAlocado)
        {
            naoAlocados.Add(new OtimizacaoNaoAlocado
            {
                NaoAlocadoId = await IdGenerator.New(),
                ResultadoId = resultadoId,
                Cliente = n.ClienteId,
                Produto = n.ProdutoId,
                LinhaProdutoId = n.LinhaProdutoId,
                VolumeM3 = (decimal)n.VolumeM3,
                DemandaM3 = (decimal)n.DemandaM3,
                Prioridade = n.Prioridade,
                Motivo = n.Motivo,
                MaiorFolgaM3 = (decimal)n.MaiorFolgaM3,
                PisoM3 = (decimal)n.PisoM3
            });
        }
        unitOfWork.OtimizacaoNaoAlocadoRepository.AddRange(naoAlocados);

        var embarques = new List<OtimizacaoEmbarque>();
        foreach (var e in execucao.Carretas.Maiores)
        {
            var semana = SemanaIso.Parse(e.Semana);
            embarques.Add(new OtimizacaoEmbarque
            {
                EmbarqueId = await IdGenerator.New(),
                ResultadoId = resultadoId,
                Cliente = e.ClienteId,
                CentroId = e.CentroId,
                Centro = e.Centro,
                Ano = semana.Ano,
                Semana = semana.Numero,
                Carretas = e.Carretas,
                VolumeM3 = (decimal)e.VolumeM3,
                OcupacaoMedia = (decimal)e.OcupacaoMedia
            });
        }
        unitOfWork.OtimizacaoEmbarqueRepository.AddRange(embarques);

        var ocupacoes = new List<OtimizacaoOcupacao>();
        foreach (var o in execucao.Ocupacao)
        {
            var semana = SemanaIso.Parse(o.Semana);
            ocupacoes.Add(new OtimizacaoOcupacao
            {
                OcupacaoId = await IdGenerator.New(),
                ResultadoId = resultadoId,
                CentroId = o.CentroId,
                Centro = o.Centro,
                Ano = semana.Ano,
                Semana = semana.Numero,
                AlocadoM3 = (decimal)o.AlocadoM3,
                CapacidadeM3 = (decimal)o.CapacidadeM3
            });
        }
        unitOfWork.OtimizacaoOcupacaoRepository.AddRange(ocupacoes);

        var criterios = new List<OtimizacaoCriterio>();
        foreach (var c in execucao.Criterios)
        {
            criterios.Add(new OtimizacaoCriterio
            {
                CriterioId = await IdGenerator.New(),
                ResultadoId = resultadoId,
                Nome = c.Nome,
                Descricao = c.Descricao,
                Ordem = c.Ordem,
                Peso = c.Peso,
                Valor = (decimal)c.Valor
            });
        }
        unitOfWork.OtimizacaoCriterioRepository.AddRange(criterios);

        await unitOfWork.SaveAsync();

        return MapearResponse(execucao, resultadoId);
    }

    private string ObterCaminhoDatasets()
    {
        var configurado = environmentVariables["Otimizador:DatasetsPath"];
        if (!string.IsNullOrWhiteSpace(configurado))
            return Path.GetFullPath(configurado);

        var baseDir = AppContext.BaseDirectory;
        var padrao = Path.Combine(baseDir, "..", "..", "..", "..", "Data", "Datasets");
        return Path.GetFullPath(padrao);
    }

    private static Config CriarConfig(OtimizacaoRequest? request)
    {
        var config = new Config();

        if (request is null) return config;

        if (request.Horizonte.HasValue) config.Horizonte = request.Horizonte.Value;
        if (request.Capacidade.HasValue) config.Capacidade = (ModoCapacidade)request.Capacidade.Value;
        if (!string.IsNullOrWhiteSpace(request.SemanaInicial)) config.SemanaInicial = request.SemanaInicial;
        if (request.AlvoCapacidadeSobreDemanda.HasValue) config.AlvoCapacidadeSobreDemanda = request.AlvoCapacidadeSobreDemanda.Value;
        if (request.LimiteSegundos.HasValue) config.LimiteSegundos = request.LimiteSegundos.Value;
        if (request.CarretaAtiva.HasValue) config.Carreta.Ativa = request.CarretaAtiva.Value;
        if (request.CarretaMinimoM3.HasValue) config.Carreta.MinimoM3 = request.CarretaMinimoM3.Value;
        if (request.CarretaMaximoM3.HasValue) config.Carreta.MaximoM3 = request.CarretaMaximoM3.Value;

        return config;
    }

    private static OtimizacaoResponse MapearResponse(ExecucaoDto execucao, string resultadoId)
    {
        return new OtimizacaoResponse
        {
            ResultadoId = resultadoId,
            GeradoEm = execucao.GeradoEm,
            Horizonte = execucao.Horizonte,
            Solver = new OtimizacaoSolverResponse
            {
                Status = execucao.Solver.Status,
                Segundos = execucao.Solver.Segundos,
                Objetivo = execucao.Solver.Objetivo,
                Variaveis = execucao.Solver.Variaveis,
                Binarias = execucao.Solver.Binarias,
                GreedyInicialM3 = execucao.Solver.GreedyInicialM3
            },
            Resumo = new OtimizacaoResumoResponse
            {
                DemandaTotalM3 = execucao.Resumo.DemandaTotalM3,
                DemandaElegivelM3 = execucao.Resumo.DemandaElegivelM3,
                ExcluidoPreflightM3 = execucao.Resumo.ExcluidoPreflightM3,
                AlocadoM3 = execucao.Resumo.AlocadoM3,
                NaoAlocadoM3 = execucao.Resumo.NaoAlocadoM3,
                CapacidadeTotal = execucao.Resumo.CapacidadeTotal,
                FatorCapacidade = execucao.Resumo.FatorCapacidade,
                PercentualAlocado = execucao.Resumo.PercentualAlocado,
                Itens = execucao.Resumo.Itens,
                ItensExcluidos = execucao.Resumo.ItensExcluidos
            },
            Ocupacao = execucao.Ocupacao.Select(o => new OtimizacaoOcupacaoResponse
            {
                CentroId = o.CentroId,
                Centro = o.Centro,
                Semana = o.Semana,
                AlocadoM3 = o.AlocadoM3,
                CapacidadeM3 = o.CapacidadeM3
            }).ToList(),
            Alocacoes = execucao.Alocacoes.Select(a => new OtimizacaoAlocacaoResponse
            {
                Cliente = a.ClienteId,
                Produto = a.ProdutoId,
                LinhaProdutoId = a.LinhaProdutoId,
                CentroId = a.CentroId,
                Centro = a.Centro,
                Semana = a.Semana,
                VolumeM3 = a.VolumeM3,
                Cif = a.Cif,
                Prioridade = a.Prioridade,
                MotivoSemana = a.MotivoSemana,
                MotivoPlanta = a.MotivoPlanta,
                FolgaAntesM3 = a.FolgaAntesM3,
                PlantasElegiveis = a.PlantasElegiveis,
                PosicaoPrioridade = a.PosicaoPrioridade
            }).ToList(),
            NaoAlocado = execucao.NaoAlocado.Select(n => new OtimizacaoNaoAlocadoResponse
            {
                Cliente = n.ClienteId,
                Produto = n.ProdutoId,
                LinhaProdutoId = n.LinhaProdutoId,
                VolumeM3 = n.VolumeM3,
                DemandaM3 = n.DemandaM3,
                Prioridade = n.Prioridade,
                Motivo = n.Motivo,
                MaiorFolgaM3 = n.MaiorFolgaM3,
                PisoM3 = n.PisoM3
            }).OrderByDescending(n => n.VolumeM3).ToList(),
            Embarques = execucao.Carretas.Maiores.Select(e => new OtimizacaoEmbarqueResponse
            {
                Cliente = e.ClienteId,
                CentroId = e.CentroId,
                Centro = e.Centro,
                Semana = e.Semana,
                Carretas = e.Carretas,
                VolumeM3 = e.VolumeM3,
                OcupacaoMedia = e.OcupacaoMedia
            }).ToList(),
            Criterios = execucao.Criterios.Select(c => new OtimizacaoCriterioResponse
            {
                Nome = c.Nome,
                Descricao = c.Descricao,
                Ordem = c.Ordem,
                Peso = c.Peso,
                Valor = c.Valor
            }).ToList(),
            Notas = execucao.Notas.ToList()
        };
    }
}
