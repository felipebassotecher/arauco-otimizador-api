using Arauco.Otimizador.Common.Domain.Models.Parametro;
using Arauco.Otimizador.Common.Domain.Services.Parametro;
using Arauco.Otimizador.Data.Entities;
using Arauco.Otimizador.Data.Entities.Parametro;
using Arauco.Otimizador.Service.Base;
using Microsoft.EntityFrameworkCore;
using Techer.Common.Domain.Exceptions;
using Techer.Common.Domain.Interfaces;
using Techer.Common.Id;

namespace Arauco.Otimizador.Service.ParametroService;

public class ParametroService : ServiceBase, IParametroService
{
    public ParametroService(IUnitOfWork unitOfWork, IEnvironmentVariables environmentVariables) : base(unitOfWork, environmentVariables)
    {
    }

    public async Task<List<ParametroListaResponse>> ListarAsync()
    {
        var parametros = await unitOfWork.ParametroRepository.AsQueryable().ToListAsync();
        var valores = await _ObterValoresAsync(parametros.Select(p => p.ParametroId).ToList());

        return parametros
            .Select(p => new ParametroListaResponse
            {
                Id = p.ParametroId,
                Nome = p.Nome,
                Chave = p.Chave,
                Descricao = p.Descricao,
                Peso = p.Peso,
                Ativo = p.Ativo,
                Valores = _MapValores(valores.Where(v => v.ParametroId == p.ParametroId).ToList())
            })
            .ToList();
    }

    public async Task<List<ParametroListaResponse>> ListarAtivosAsync()
    {
        var parametros = await unitOfWork.ParametroRepository.Where(p => p.Ativo).ToListAsync();
        var valores = await _ObterValoresAsync(parametros.Select(p => p.ParametroId).ToList());

        return parametros
            .Select(p => new ParametroListaResponse
            {
                Id = p.ParametroId,
                Nome = p.Nome,
                Chave = p.Chave,
                Descricao = p.Descricao,
                Peso = p.Peso,
                Ativo = p.Ativo,
                Valores = _MapValores(valores.Where(v => v.ParametroId == p.ParametroId).ToList())
            })
            .ToList();
    }

    public async Task<ParametroDetalheResponse> ObterAsync(string parametroId)
    {
        var parametro = await _ObterParametroAsync(parametroId);
        var valores = await _ObterValoresAsync(new List<string> { parametroId });

        return new ParametroDetalheResponse
        {
            Id = parametro.ParametroId,
            Nome = parametro.Nome,
            Chave = parametro.Chave,
            Descricao = parametro.Descricao,
            Peso = parametro.Peso,
            Ativo = parametro.Ativo,
            Valores = _MapValores(valores)
        };
    }

    public async Task<ParametroCriacaoResponse> CriarAsync(ParametroCriacaoRequest model)
    {
        if (await unitOfWork.ParametroRepository.AnyAsync(p => p.Chave == model.Chave))
            throw new ApiException("Já existe um parâmetro com essa chave");

        var parametro = new Parametro
        {
            ParametroId = await IdGenerator.New(),
            Nome = model.Nome,
            Chave = model.Chave,
            Descricao = model.Descricao,
            Peso = model.Peso,
            Ativo = model.Ativo
        };

        unitOfWork.ParametroRepository.Add(parametro);

        _AdicionarValores(parametro.ParametroId, model.Valores);

        await unitOfWork.SaveAsync();

        var valores = await _ObterValoresAsync(new List<string> { parametro.ParametroId });

        return new ParametroCriacaoResponse
        {
            Id = parametro.ParametroId,
            Nome = parametro.Nome,
            Chave = parametro.Chave,
            Descricao = parametro.Descricao,
            Peso = parametro.Peso,
            Ativo = parametro.Ativo,
            Valores = _MapValores(valores)
        };
    }

    public async Task<ParametroAtualizacaoResponse> AtualizarAsync(string parametroId, ParametroAtualizacaoRequest model)
    {
        var parametro = await _ObterParametroAsync(parametroId);

        if (await unitOfWork.ParametroRepository.AnyAsync(p => p.Chave == model.Chave && p.ParametroId != parametroId))
            throw new ApiException("Já existe um parâmetro com essa chave");

        parametro.Nome = model.Nome;
        parametro.Chave = model.Chave;
        parametro.Descricao = model.Descricao;
        parametro.Peso = model.Peso;
        parametro.Ativo = model.Ativo;

        var valoresAtuais = await unitOfWork.ParametroValorRepository.Where(v => v.ParametroId == parametroId).ToListAsync();
        unitOfWork.ParametroValorRepository.RemoveRange(valoresAtuais);

        _AdicionarValores(parametroId, model.Valores);

        await unitOfWork.SaveAsync();

        var valores = await _ObterValoresAsync(new List<string> { parametroId });

        return new ParametroAtualizacaoResponse
        {
            Id = parametro.ParametroId,
            Nome = parametro.Nome,
            Chave = parametro.Chave,
            Descricao = parametro.Descricao,
            Peso = parametro.Peso,
            Ativo = parametro.Ativo,
            Valores = _MapValores(valores)
        };
    }

    public async Task RemoverAsync(string parametroId)
    {
        var parametro = await _ObterParametroAsync(parametroId);

        var valores = await unitOfWork.ParametroValorRepository.Where(v => v.ParametroId == parametroId).ToListAsync();
        unitOfWork.ParametroValorRepository.RemoveRange(valores);

        var vinculos = await unitOfWork.CenarioParametroRepository.Where(c => c.ParametroId == parametroId).ToListAsync();
        unitOfWork.CenarioParametroRepository.RemoveRange(vinculos);

        unitOfWork.ParametroRepository.Remove(parametro);

        await unitOfWork.SaveAsync();
    }

    private async Task<Parametro> _ObterParametroAsync(string parametroId)
    {
        return await unitOfWork
            .ParametroRepository
            .FirstOrDefaultAsync(p => p.ParametroId == parametroId) ?? throw new NotFoundException("Parâmetro não encontrado");
    }

    private void _AdicionarValores(string parametroId, List<ParametroValorRequest>? valores)
    {
        if (valores == null)
            return;

        foreach (var valor in valores)
        {
            unitOfWork.ParametroValorRepository.Add(new ParametroValor
            {
                ParametroId = parametroId,
                Valor = valor.Valor,
                Rotulo = valor.Rotulo,
                Peso = valor.Peso
            });
        }
    }

    private async Task<List<ParametroValor>> _ObterValoresAsync(List<string> parametroIds)
    {
        return await unitOfWork
            .ParametroValorRepository
            .Where(v => parametroIds.Contains(v.ParametroId))
            .ToListAsync();
    }

    private static List<ParametroValorResponse>? _MapValores(List<ParametroValor> valores)
    {
        if (valores.Count == 0)
            return null;

        return valores.Select(v => new ParametroValorResponse
        {
            Valor = v.Valor,
            Rotulo = v.Rotulo,
            Peso = v.Peso
        }).ToList();
    }
}
