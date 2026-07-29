using Amazon.Lambda.Core;
using Arauco.Function.Senior.Models;
using Arauco.Otimizador.Aws.Shared;
using Arauco.Otimizador.Common.Domain.Enums;
using Arauco.Otimizador.Data.Entities;
using Arauco.Otimizador.Data.Entities.Colaborador;
using Arauco.Otimizador.Data.MySql;
using Arauco.Otimizador.Function.Base;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using Techer.Aws.Cognito;
using Techer.Common.Extensions;

namespace Arauco.Otimizador.Function.Senior;

public class Function : BaseFunction
{
    // Db
    private readonly static ISeniorUnitOfWork unitOfWork = new SeniorUnitOfWork(SeniorDbContext.Create());
    private readonly static CognitoHelper cognitoHelper = new CognitoHelper(environmentVariables, Cognitos.App);

    public async Task FunctionHandler(ILambdaContext context)
    {
        var ws = new WsSenior.rubi_Syncbr_com_arauco_rh_dadosColaboradorClient();

        ws.InnerChannel.OperationTimeout = new TimeSpan(0, 5, 0);

        context.Logger.LogInformation($"Baixando informacoes do Senior");

        var resultadoWs = await ws.get_dados_colaboradorAsync("suporte.cloud", "Arauco@2025", 0, new WsSenior.dadosColaboradorgetdadoscolaboradorIn
        {
            numEmp = "3,19,22",
            tipCol = "1,2"
        });

        if (!string.IsNullOrEmpty(resultadoWs.erroExecucao))
        {
            throw new Exception("Erro ao obter dados: " + resultadoWs.erroExecucao);
        }

        var dadosFuncionarios = resultadoWs
            .dados
            .Select(d => new ColaboradorSeniorModel
            {
                Matricula = d.numCadRet.ToString().GetNumbers().PadLeft(12, '0'),
                NumeroCracha = d.numCra.ToString().GetNumbers(),
                Nome = d.nomFun,
                DataNascimento = string.IsNullOrEmpty(d.datNas) ? null : DateOnly.ParseExact(d.datNas, "dd/MM/yyyy", CultureInfo.InvariantCulture),
                Celular = ParseTelefone(d.ddd1, d.numCel),
                Telefone2 = ParseTelefone(d.DDD2, d.telefone_2),
                CentroCusto = d.codCcu.Length > 3 ? d.codCcu : null,
                Ferias = d.ferias == "S",
                FilialCodigo = d.codFilRet,
                FilialNome = d.desFil,
                MatriculaGestor = d.matGes.HasValue && d.matGes.Value > 0 ? d.matGes.Value.ToString().PadLeft(12, '0') : null,
                NumeroEmpresa = d.numEmpRet,
                CodigoPostoTrabalho = d.posTra,
                PostoTrabalhoNome = d.desPos,
                Status = d.status,
                Genero = d.genCol,
                Cpf = d.cpf != null ? d.cpf.GetNumbers().PadLeft(11, '0') : null,
                TipoColaborador = d.tipCol,
                EmailComercial = d.email_comercial,
                EmailParticular = d.email_particular,
                CodigoCargo = d.codigo_cargo,
                DescricaoCargo = d.descricao_cargo
            }).ToList();

        // Filtros - Campos Obrigatorios
        dadosFuncionarios = dadosFuncionarios
            .Where(d => d.NumeroEmpresa.HasValue &&
                            d.FilialCodigo.HasValue &&
                            d.Cpf != null)
            .ToList();

        // Tabelas Relacionadas
        context.Logger.LogInformation($"Atualizacao tabelas relacionadas");

        await ProcessarCargoAsync(unitOfWork, dadosFuncionarios);

        await ProcessarSituacaoColaboradorAsync(unitOfWork, dadosFuncionarios);

        // Colaboradores
        context.Logger.LogInformation($"Colaboradores");

        var universoColaboradores = await unitOfWork
            .ColaboradorRepository
            .Where(c => c.TipoColaboradorEnum == TipoColaboradorEnum.Empregado)
            .ToListAsync();

        // Atualizacao
        context.Logger.LogInformation($"Atualizacao Empregados");

        var empregados = dadosFuncionarios
            .Where(d => d.TipoColaborador == 1)
            .ToList();

        await ProcessarColaboradoresAsync(unitOfWork, context, TipoColaboradorEnum.Empregado, empregados, universoColaboradores);

        // Terceiros
        context.Logger.LogInformation($"Terceiros");

        var universoTerceiros = await unitOfWork
            .ColaboradorRepository
            .Where(c => c.TipoColaboradorEnum == TipoColaboradorEnum.Terceiro)
            .ToListAsync();

        // Atualizacao
        context.Logger.LogInformation($"Atualizacao Terceiros");

        var terceiros = dadosFuncionarios
            .Where(d => d.TipoColaborador == 2 && d.NumeroEmpresa == 22 && d.FilialCodigo == 5)
            .ToList();

        await ProcessarColaboradoresAsync(unitOfWork, context, TipoColaboradorEnum.Terceiro, terceiros, universoTerceiros);
    }

    private string? ParseTelefone(string ddd, string numero)
    {
        string? res = null;

        if (ddd != null && numero != null && !string.IsNullOrWhiteSpace(ddd.GetNumbers()) && !string.IsNullOrWhiteSpace(numero.GetNumbers()))
        {
            ddd = ddd.GetNumbers();
            numero = numero.GetNumbers();

            if (int.TryParse(ddd, out int tmpDdd) && tmpDdd > 9 && tmpDdd < 100)
                if (long.TryParse(numero, out long tmpNumero) && tmpNumero > 0)
                    res = ddd + numero;
        }

        return res;
    }

    private async Task ProcessarSituacaoColaboradorAsync(ISeniorUnitOfWork unitOfWork, List<ColaboradorSeniorModel> dadosFuncionarios)
    {
        var situacoes = await unitOfWork
            .SituacaoColaboradorRepository
            .AsQueryable()
            .ToListAsync();

        var grupos = dadosFuncionarios
            .Where(d => !string.IsNullOrWhiteSpace(d.Status))
            .GroupBy(d => d.Status)
            .Select(d => new
            {
                Status = d.Key,
                Funcionarios = d.ToList()
            });

        foreach (var grupo in grupos)
        {
            var situacao = situacoes
                .Where(c => c.Codigo == grupo.Status)
                .FirstOrDefault();

            if (situacao == null)
            {
                situacao = new SituacaoColaborador
                {
                    Codigo = grupo.Status
                };

                unitOfWork.SituacaoColaboradorRepository.Add(situacao);
            }

            situacao.Nome = grupo.Status;
            await unitOfWork.SaveAsync();

            foreach (var func in grupo.Funcionarios)
            {
                func.SituacaoColaboradorId = situacao.SituacaoColaboradorId;
            }
        }
    }

    private async Task ProcessarCargoAsync(ISeniorUnitOfWork unitOfWork, List<ColaboradorSeniorModel> dadosFuncionarios)
    {
        var cargos = await unitOfWork
            .CargoRepository
            .AsQueryable()
            .ToListAsync();

        var grupos = dadosFuncionarios
            .Where(d => !string.IsNullOrWhiteSpace(d.CodigoCargo) && !string.IsNullOrWhiteSpace(d.DescricaoCargo))
            .GroupBy(d => d.CodigoCargo)
            .Select(d => new
            {
                CodigoCargo = d.Key,
                Funcionarios = d.ToList()
            });

        foreach (var grupo in grupos)
        {
            if (!int.TryParse(grupo.CodigoCargo, out int cargoId))
                continue;

            var cargo = cargos
                .Where(c => c.CargoId == cargoId)
                .FirstOrDefault();

            if (cargo == null)
            {
                cargo = new Cargo
                {
                    CargoId = cargoId
                };

                unitOfWork.CargoRepository.Add(cargo);
            }

            cargo.Nome = grupo.Funcionarios.First().DescricaoCargo;
            await unitOfWork.SaveAsync();

            foreach (var func in grupo.Funcionarios)
            {
                func.CargoId = cargo.CargoId;
            }
        }
    }

    private async Task ProcessarColaboradoresAsync(ISeniorUnitOfWork unitOfWork, ILambdaContext context, TipoColaboradorEnum tipoColaborador, List<ColaboradorSeniorModel> dadosFuncionarios, List<Colaborador> universoColaboradores)
    {
        var cpfs = new List<string>();
        foreach (var batch in dadosFuncionarios.Batch(50))
        {
            foreach (var item in batch)
            {
                cpfs.Add(item.Cpf);

                var colab = universoColaboradores
                    .FirstOrDefault(f => f.Cpf == item.Cpf);

                if (colab == null)
                {
                    colab = new Colaborador
                    {
                        TipoColaboradorEnum = tipoColaborador,
                        Ativo = true,
                        Excluido = false
                    };

                    unitOfWork.ColaboradorRepository.Add(colab);
                    universoColaboradores.Add(colab);
                }

                colab.Nome = item.Nome;
                colab.Cpf = item.Cpf;
                colab.EmailComercial = !string.IsNullOrWhiteSpace(item.EmailComercial) ? item.EmailComercial.Trim().ToLower() : null;
                colab.EmailParticular = !string.IsNullOrWhiteSpace(item.EmailParticular) ? item.EmailParticular.Trim().ToLower() : null;
                colab.Matricula = item.Matricula;
                colab.DataNascimento = item.DataNascimento;
                colab.Celular = item.Celular?.GetNumbers();
                colab.Telefone2 = item.Telefone2?.GetNumbers();
                colab.CargoId = item.CargoId;
                colab.Ferias = item.Ferias;
                colab.Genero = item.Genero;
                colab.NumeroCracha = item.NumeroCracha;
                colab.SituacaoColaboradorId = item.SituacaoColaboradorId;
                colab.Excluido = false;
                colab.Ativo = item.Status == "Ativo";
            }

            await unitOfWork.SaveAsync();
        }

        // Remocacao
        context.Logger.LogInformation($"Remocao");

        var exclusao = universoColaboradores
            .Where(c => !cpfs.Contains(c.Cpf))
            .ToList();

        foreach (var item in exclusao)
        {
            item.Excluido = true;
        }

        await unitOfWork.SaveAsync();

        // Gestores
        context.Logger.LogInformation($"Gestores");

        foreach (var batch in dadosFuncionarios.Batch(50))
        {
            foreach (var item in batch)
            {
                var colab = universoColaboradores
                    .Where(u => u.Cpf == item.Cpf)
                    .FirstOrDefault();

                if (colab != null)
                {
                    if (item.MatriculaGestor != null)
                    {
                        var gestor = universoColaboradores
                            .Where(u => u.Matricula == item.MatriculaGestor)
                            .FirstOrDefault();

                        if (gestor != null)
                        {
                            colab.ColaboradorId_Gestor = gestor.ColaboradorId;
                        }
                    }
                    else
                    {
                        colab.ColaboradorId_Gestor = null;
                    }
                }
            }

            await unitOfWork.SaveAsync();
        }

        // Cognito
        context.Logger.LogInformation($"Usuarios");

        int n = 0;
        foreach (var colab in universoColaboradores)
        {
            if (colab.Cpf == null || !colab.DataNascimento.HasValue)
                continue;

            try
            {
                var ativo = !colab.Excluido && colab.Ativo;

                var username = $"COLAB#{colab.ColaboradorId}";
                var preferedName = colab.Cpf;
                var password = $"{colab.DataNascimento:ddMMyy}";

                var user = await cognitoHelper.GetUserAsync(username);

                if (user == null && ativo)
                {
                    await cognitoHelper.CreateAsync(
                        username,
                        preferedName,
                        password);

                    colab.Cognito = true;
                }
                else if (user != null)
                {
                    colab.Cognito = true;

                    if (user.Enabled != ativo)
                    {
                        await cognitoHelper.SetStatus(username, ativo);
                    }
                }
            }
            catch
            {
                context.Logger.LogLine($"CPF: {colab.Cpf} - {colab.ColaboradorId} - {colab.DataNascimento:ddMMyy}");
            }

            n++;
            if (n % 100 == 0)
            {
                await unitOfWork.SaveAsync();

                context.Logger.LogLine($"+ {n}");
            }
        }

        await unitOfWork.SaveAsync();

        context.Logger.LogLine($"+ {n}");
    }
}