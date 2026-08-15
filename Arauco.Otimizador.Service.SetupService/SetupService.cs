using Arauco.Otimizador.Common.Domain.Enums.Setup;
using Arauco.Otimizador.Common.Domain.Models.Setup;
using Arauco.Otimizador.Common.Domain.Services.Setup;
using Arauco.Otimizador.Data.Entities;
using Arauco.Otimizador.Data.Entities.Setup;
using Arauco.Otimizador.Service.Base;
using Microsoft.EntityFrameworkCore;
using Techer.Common.Domain.Exceptions;
using Techer.Common.Domain.Interfaces;
using Techer.Common.Id;

namespace Arauco.Otimizador.Service.SetupService;

public class SetupService : ServiceBase, ISetupService
{
    // Descrições padrão dos critérios de ordem de importância. São persistidas no banco junto com cada
    // setup para que a interface possa explicar o critério sem depender de texto em código do front.
    private static readonly Dictionary<CriterioOrdemEnum, string> DescricoesCriterio = new()
    {
        [CriterioOrdemEnum.PriorizarFreteCIF] =
            "No momento de priorizar os pedidos, leva em consideração o tipo de frete do cliente, e, caso seja CIF, o pedido é priorizado para entrega com a maior antecedência possível.",
        [CriterioOrdemEnum.PiorizarClienteRevenda] =
            "No momento de priorizar os pedidos, identifica clientes do segmento Revenda e reduz a prioridade de entrega desses pedidos.",
        [CriterioOrdemEnum.Antecipar] =
            "Prioriza a antecipação das entregas, buscando alocar os pedidos nas semanas mais próximas possíveis, respeitando a data de entrega desejada.",
        [CriterioOrdemEnum.AtenderDemanda] =
            "Busca maximizar o volume total de demanda atendida no cenário, priorizando o cumprimento da quantidade solicitada.",
        [CriterioOrdemEnum.PedidoMaisAntigo] =
            "Considera a antiguidade do pedido como critério de desempate, dando prioridade aos pedidos registrados há mais tempo."
    };

    public SetupService(IUnitOfWork unitOfWork, IEnvironmentVariables environmentVariables) : base(unitOfWork, environmentVariables)
    {
    }

    public async Task<List<SetupListaResponse>> ListarAsync()
    {
        var setups = await unitOfWork.SetupRepository.AsQueryable().ToListAsync();
        var ordens = await unitOfWork.SetupOrdemImportanciaRepository.AsQueryable().ToListAsync();

        return setups
            .OrderByDescending(s => s.DataCriacao)
            .Select(s => new SetupListaResponse
            {
                Id = s.SetupId,
                Nome = s.Nome,
                Descricao = s.Descricao,
                DataCriacao = s.DataCriacao,
                DataAlteracao = s.DataAlteracao,
                PossuiOrdemImportancia = ordens.Any(o => o.SetupId == s.SetupId)
            })
            .ToList();
    }

    public async Task<SetupDetalheResponse> ObterAsync(string setupId)
    {
        var setup = await _ObterSetupAsync(setupId);
        return await _MapDetalheAsync(setup);
    }

    public async Task<SetupCriacaoResponse> CriarAsync(SetupCriacaoRequest model)
    {
        _ValidarModelo(model);

        var setup = new Setup
        {
            SetupId = await IdGenerator.New(),
            Nome = model.Nome,
            Descricao = model.Descricao,
            VolumeMinimoCarreta = model.VolumeMinimoCarreta,
            VolumeMaximoCarreta = model.VolumeMaximoCarreta,
            QuantidadeMinimaSkuPorLote = model.QuantidadeMinimaSkuPorLote,
            CapacidadeMaximaRecebimentoCliente = model.CapacidadeMaximaRecebimentoCliente,
            MixTipoFrete = model.MixTipoFrete,
            DataCriacao = DateTime.UtcNow,
            DataAlteracao = null
        };

        unitOfWork.SetupRepository.Add(setup);

        _PersistirOrdemImportancia(setup.SetupId, model.OrdemImportancia);

        await unitOfWork.SaveAsync();

        return new SetupCriacaoResponse { Id = setup.SetupId };
    }

    public async Task<SetupDetalheResponse> AtualizarAsync(string setupId, SetupAtualizacaoRequest model)
    {
        _ValidarModelo(model);

        var setup = await _ObterSetupAsync(setupId);

        setup.Nome = model.Nome;
        setup.Descricao = model.Descricao;
        setup.VolumeMinimoCarreta = model.VolumeMinimoCarreta;
        setup.VolumeMaximoCarreta = model.VolumeMaximoCarreta;
        setup.QuantidadeMinimaSkuPorLote = model.QuantidadeMinimaSkuPorLote;
        setup.CapacidadeMaximaRecebimentoCliente = model.CapacidadeMaximaRecebimentoCliente;
        setup.MixTipoFrete = model.MixTipoFrete;
        setup.DataAlteracao = DateTime.UtcNow;

        var ordensExistentes = await unitOfWork
            .SetupOrdemImportanciaRepository
            .Where(o => o.SetupId == setupId)
            .ToListAsync();
        unitOfWork.SetupOrdemImportanciaRepository.RemoveRange(ordensExistentes);

        _PersistirOrdemImportancia(setupId, model.OrdemImportancia);

        await unitOfWork.SaveAsync();

        return await _MapDetalheAsync(setup);
    }

    private async Task<Setup> _ObterSetupAsync(string setupId)
    {
        return await unitOfWork
            .SetupRepository
            .FirstOrDefaultAsync(s => s.SetupId == setupId) ?? throw new NotFoundException("Setup não encontrado");
    }

    private async Task<SetupDetalheResponse> _MapDetalheAsync(Setup setup)
    {
        var ordens = await unitOfWork
            .SetupOrdemImportanciaRepository
            .Where(o => o.SetupId == setup.SetupId)
            .OrderBy(o => o.Ordem)
            .ToListAsync();

        return new SetupDetalheResponse
        {
            Id = setup.SetupId,
            Nome = setup.Nome,
            Descricao = setup.Descricao,
            VolumeMinimoCarreta = setup.VolumeMinimoCarreta,
            VolumeMaximoCarreta = setup.VolumeMaximoCarreta,
            QuantidadeMinimaSkuPorLote = setup.QuantidadeMinimaSkuPorLote,
            CapacidadeMaximaRecebimentoCliente = setup.CapacidadeMaximaRecebimentoCliente,
            MixTipoFrete = setup.MixTipoFrete,
            OrdemImportancia = ordens.Select(o => new SetupOrdemImportanciaResponse
            {
                Criterio = o.CriterioEnum,
                Ordem = o.Ordem,
                Descricao = o.Descricao,
                Ativo = o.Ativo
            }).ToList(),
            DataCriacao = setup.DataCriacao,
            DataAlteracao = setup.DataAlteracao
        };
    }

    private static void _ValidarModelo(SetupCriacaoRequest model)
    {
        if (string.IsNullOrWhiteSpace(model.Nome))
            throw new ApiException("Nome é obrigatório");

        if (model.OrdemImportancia == null || model.OrdemImportancia.Count == 0)
            throw new ApiException("Ordem de importância deve conter ao menos um critério");

        _ValidarCamposOperacionais(model.VolumeMinimoCarreta, model.VolumeMaximoCarreta, model.MixTipoFrete);
        _ValidarOrdemImportancia(model.OrdemImportancia);
    }

    private static void _ValidarModelo(SetupAtualizacaoRequest model)
    {
        if (string.IsNullOrWhiteSpace(model.Nome))
            throw new ApiException("Nome é obrigatório");

        if (model.OrdemImportancia == null || model.OrdemImportancia.Count == 0)
            throw new ApiException("Ordem de importância deve conter ao menos um critério");

        _ValidarCamposOperacionais(model.VolumeMinimoCarreta, model.VolumeMaximoCarreta, model.MixTipoFrete);
        _ValidarOrdemImportancia(model.OrdemImportancia);
    }

    private static void _ValidarCamposOperacionais(decimal? volumeMinimo, decimal? volumeMaximo, int? mixTipoFrete)
    {
        if (volumeMinimo.HasValue && volumeMinimo.Value < 0)
            throw new ApiException("Volume mínimo da carreta não pode ser negativo");

        if (volumeMaximo.HasValue && volumeMaximo.Value < 0)
            throw new ApiException("Volume máximo da carreta não pode ser negativo");

        if (volumeMinimo.HasValue && volumeMaximo.HasValue && volumeMinimo.Value > volumeMaximo.Value)
            throw new ApiException("Volume mínimo da carreta não pode ser maior que o volume máximo");

        if (mixTipoFrete.HasValue && (mixTipoFrete.Value < 0 || mixTipoFrete.Value > 100))
            throw new ApiException("Mix de tipo de frete deve estar entre 0 e 100");
    }

    private static void _ValidarOrdemImportancia(List<SetupOrdemImportanciaRequest> ordemImportancia)
    {
        var valores = Enum.GetValues<CriterioOrdemEnum>();
        var ordensEsperadas = Enumerable.Range(1, ordemImportancia.Count).ToList();
        var ordensRecebidas = ordemImportancia.Select(o => o.Ordem).OrderBy(o => o).ToList();

        if (!ordensEsperadas.SequenceEqual(ordensRecebidas))
            throw new ApiException("Ordem de importância deve ser uma sequência contígua iniciando em 1");

        var criterios = ordemImportancia.Select(o => o.Criterio).ToList();
        if (criterios.Count != criterios.Distinct().Count())
            throw new ApiException("Critérios de ordem de importância não podem se repetir");

        foreach (var criterio in criterios)
        {
            if (!valores.Contains(criterio))
                throw new ApiException($"Critério de ordem '{criterio}' não é válido");
        }
    }

    private void _PersistirOrdemImportancia(string setupId, List<SetupOrdemImportanciaRequest> ordemImportancia)
    {
        foreach (var item in ordemImportancia.OrderBy(o => o.Ordem))
        {
            unitOfWork.SetupOrdemImportanciaRepository.Add(new SetupOrdemImportancia
            {
                SetupId = setupId,
                CriterioEnum = item.Criterio,
                Ordem = item.Ordem,
                Descricao = !string.IsNullOrWhiteSpace(item.Descricao)
                    ? item.Descricao
                    : DescricoesCriterio.GetValueOrDefault(item.Criterio),
                Ativo = item.Ativo
            });
        }
    }
}
