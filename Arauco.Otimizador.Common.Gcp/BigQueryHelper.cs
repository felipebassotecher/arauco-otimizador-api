using Google.Cloud.BigQuery.V2;
using Techer.Common.Domain.Interfaces;

namespace Arauco.Otimizador.Common.Gcp;

// Executa consultas no BigQuery a partir de uma service account do Google Cloud Platform (GCP). As
// tabelas consultadas normalmente são views/tabelas materializadas pelo Dataform (arquivos .sqlx) —
// este helper só executa SQL contra o resultado já materializado; não interage com a API do Dataform
// em si (não dispara execuções de pipeline).
//
// Credenciais são lidas de appsettings.json (placeholder hoje — troque pelos valores reais do GCP
// antes de usar em qualquer ambiente):
//   "Gcp": {
//     "ProjectId": "SEU_PROJETO_GCP_AQUI",
//     "CredentialsJson": "COLE_AQUI_O_JSON_COMPLETO_DA_SERVICE_ACCOUNT_DO_GCP"
//   }
// `CredentialsJson` é o conteúdo integral do arquivo de chave JSON da service account (Console GCP >
// IAM & Admin > Service Accounts > Keys > Create new key, formato JSON) — cole o JSON inteiro como
// valor da configuração (ou troque `CriarClienteAsync` para ler de um arquivo/secret manager).
public static class BigQueryHelper
{
    private const string ProjectIdConfigKey = "Gcp:ProjectId";
    private const string CredentialsJsonConfigKey = "Gcp:CredentialsJson";

    public static async Task<BigQueryResults> ExecutarConsultaAsync(IEnvironmentVariables env, string sql)
    {
        var client = await CriarClienteAsync(env);

        return await client.ExecuteQueryAsync(sql, parameters: null);
    }

    private static async Task<BigQueryClient> CriarClienteAsync(IEnvironmentVariables env)
    {
        var projectId = ObterConfiguracaoObrigatoria(env, ProjectIdConfigKey);
        var credentialsJson = ObterConfiguracaoObrigatoria(env, CredentialsJsonConfigKey);

        var builder = new BigQueryClientBuilder
        {
            ProjectId = projectId,
            JsonCredentials = credentialsJson,
        };

        return await builder.BuildAsync();
    }

    private static string ObterConfiguracaoObrigatoria(IEnvironmentVariables env, string chave)
    {
        var valor = env[chave];

        if (string.IsNullOrWhiteSpace(valor))
            throw new InvalidOperationException(
                $"Configuração '{chave}' ausente. Configure-a em appsettings.json (hoje é só um " +
                "placeholder) com as credenciais reais do GCP antes de usar esta integração.");

        return valor;
    }
}
