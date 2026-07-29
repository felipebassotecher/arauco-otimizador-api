using Amazon.Lambda.Core;
using Arauco.Otimizador.Aws.Shared;
using Arauco.Otimizador.Data.MySql;
using Arauco.Otimizador.Function.Base;
using Microsoft.EntityFrameworkCore;
using Techer.Aws.Storage;
using Techer.Common.Json;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Arauco.Otimizador.Function.DataSource;

public class Function : BaseFunction
{
    // Db
    private static readonly HubDbContext dbContext = HubDbContext.Create();
    private static readonly UnitOfWork unitOfWork = new(dbContext);

    public async Task FunctionHandler(ILambdaContext context)
    {
        context.Logger.LogLine($"Iniciando processamento data source");

        await FuncionariosAsync();
    }

    private async Task FuncionariosAsync()
    {
        var regs = await unitOfWork
            .FuncionarioRepository
            .AsQueryable()
            .Select(f => new
            {
                f.FuncionarioId,
                f.FuncionarioId_Lider,
                f.CentroCustoId,
                f.DataHoraAtualizacaoLider,
                f.DataNascimento,
                f.EmpresaId,
                f.FilialId,
                f.FuncionarioId_AtualizacaoLider,
                f.FuncionarioId_Gestor,
                f.Genero,
                f.IndicadorFerias,
                f.Nome,
                f.NumeroCelular,
                f.NumeroCracha,
                f.NumeroWhatsApp,
                f.PostoTrabalhoId,
                f.RestricoesAlimentares,
            }).ToListAsync();

        var json = JsonHelper.Serialize(regs);

        await S3Helper.UploadFile(
            environmentVariables,
            Buckets.DataSource,
            "funcionarios.json",
            System.Text.UTF8Encoding.UTF8.GetBytes(json),
            "application/json");
    }

}
