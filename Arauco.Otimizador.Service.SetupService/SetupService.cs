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
            PesoMinimoCarregamento = model.PesoMinimoCarregamento,
            PercentualVariacaoMediaVenda = model.PercentualVariacaoMediaVenda,
            QuantidadeMaximaTrocas = model.QuantidadeMaximaTrocas,
            UtilizarToleranciaPeso = model.UtilizarToleranciaPeso,
            PermitirCarregarAbaixoPesoMinimo = model.PermitirCarregarAbaixoPesoMinimo,
            PriorizarTipoFrete = model.PriorizarTipoFrete,
            TipoFreteEnum = model.TipoFrete,
            PriorizarTipoCliente = model.PriorizarTipoCliente,
            TipoClienteEnum = model.TipoCliente,
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
        setup.PesoMinimoCarregamento = model.PesoMinimoCarregamento;
        setup.PercentualVariacaoMediaVenda = model.PercentualVariacaoMediaVenda;
        setup.QuantidadeMaximaTrocas = model.QuantidadeMaximaTrocas;
        setup.UtilizarToleranciaPeso = model.UtilizarToleranciaPeso;
        setup.PermitirCarregarAbaixoPesoMinimo = model.PermitirCarregarAbaixoPesoMinimo;
        setup.PriorizarTipoFrete = model.PriorizarTipoFrete;
        setup.TipoFreteEnum = model.TipoFrete;
        setup.PriorizarTipoCliente = model.PriorizarTipoCliente;
        setup.TipoClienteEnum = model.TipoCliente;
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

    public async Task<SetupDetalheResponse> ClonarAsync(string setupId)
    {
        var origem = await _ObterSetupAsync(setupId);
        var ordensOrigem = await unitOfWork
            .SetupOrdemImportanciaRepository
            .Where(o => o.SetupId == setupId)
            .ToListAsync();

        var novoSetup = new Setup
        {
            SetupId = await IdGenerator.New(),
            Nome = $"{origem.Nome} (cópia)",
            Descricao = origem.Descricao,
            PesoMinimoCarregamento = origem.PesoMinimoCarregamento,
            PercentualVariacaoMediaVenda = origem.PercentualVariacaoMediaVenda,
            QuantidadeMaximaTrocas = origem.QuantidadeMaximaTrocas,
            UtilizarToleranciaPeso = origem.UtilizarToleranciaPeso,
            PermitirCarregarAbaixoPesoMinimo = origem.PermitirCarregarAbaixoPesoMinimo,
            PriorizarTipoFrete = origem.PriorizarTipoFrete,
            TipoFreteEnum = origem.TipoFreteEnum,
            PriorizarTipoCliente = origem.PriorizarTipoCliente,
            TipoClienteEnum = origem.TipoClienteEnum,
            DataCriacao = DateTime.UtcNow,
            DataAlteracao = null
        };

        unitOfWork.SetupRepository.Add(novoSetup);

        foreach (var ordem in ordensOrigem.OrderBy(o => o.Ordem))
        {
            unitOfWork.SetupOrdemImportanciaRepository.Add(new SetupOrdemImportancia
            {
                SetupId = novoSetup.SetupId,
                CriterioEnum = ordem.CriterioEnum,
                Ordem = ordem.Ordem
            });
        }

        await unitOfWork.SaveAsync();

        return await _MapDetalheAsync(novoSetup);
    }

    public async Task RemoverAsync(string setupId)
    {
        var setup = await _ObterSetupAsync(setupId);

        var ordens = await unitOfWork.SetupOrdemImportanciaRepository.Where(o => o.SetupId == setupId).ToListAsync();
        unitOfWork.SetupOrdemImportanciaRepository.RemoveRange(ordens);

        unitOfWork.SetupRepository.Remove(setup);

        await unitOfWork.SaveAsync();
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
            PesoMinimoCarregamento = setup.PesoMinimoCarregamento,
            PercentualVariacaoMediaVenda = setup.PercentualVariacaoMediaVenda,
            QuantidadeMaximaTrocas = setup.QuantidadeMaximaTrocas,
            UtilizarToleranciaPeso = setup.UtilizarToleranciaPeso,
            PermitirCarregarAbaixoPesoMinimo = setup.PermitirCarregarAbaixoPesoMinimo,
            PriorizarTipoFrete = setup.PriorizarTipoFrete,
            TipoFrete = setup.TipoFreteEnum,
            PriorizarTipoCliente = setup.PriorizarTipoCliente,
            TipoCliente = setup.TipoClienteEnum,
            OrdemImportancia = ordens.Select(o => new SetupOrdemImportanciaResponse
            {
                Criterio = o.CriterioEnum,
                Ordem = o.Ordem
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

        _ValidarOrdemImportancia(model.OrdemImportancia);

        if (model.PriorizarTipoFrete && !model.TipoFrete.HasValue)
            throw new ApiException("Tipo de frete é obrigatório quando 'Priorizar tipo de frete' está ativo");

        if (model.PriorizarTipoCliente && !model.TipoCliente.HasValue)
            throw new ApiException("Tipo de cliente é obrigatório quando 'Priorizar tipo de cliente' está ativo");
    }

    private static void _ValidarModelo(SetupAtualizacaoRequest model)
    {
        if (string.IsNullOrWhiteSpace(model.Nome))
            throw new ApiException("Nome é obrigatório");

        if (model.OrdemImportancia == null || model.OrdemImportancia.Count == 0)
            throw new ApiException("Ordem de importância deve conter ao menos um critério");

        _ValidarOrdemImportancia(model.OrdemImportancia);

        if (model.PriorizarTipoFrete && !model.TipoFrete.HasValue)
            throw new ApiException("Tipo de frete é obrigatório quando 'Priorizar tipo de frete' está ativo");

        if (model.PriorizarTipoCliente && !model.TipoCliente.HasValue)
            throw new ApiException("Tipo de cliente é obrigatório quando 'Priorizar tipo de cliente' está ativo");
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
                Ordem = item.Ordem
            });
        }
    }
}
