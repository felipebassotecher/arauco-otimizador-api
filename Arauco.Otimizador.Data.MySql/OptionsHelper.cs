using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Techer.Aws.Secrets;

namespace Arauco.Otimizador.Data.MySql
{
    public enum MySqlSecretOption
    {
        Default = 1
    };

    public static class OptionsHelper
    {
        public static DbContextOptionsBuilder UseMySqlWithSecrets(this DbContextOptionsBuilder options, MySqlSecretOption secretOption = MySqlSecretOption.Default)
        {
            var secrets = new SecretsHelper();

            var secretName = GetSecretName(secretOption);
            var secret = secrets.GetSecret(secretName);

            var parametros = JsonConvert.DeserializeObject<Dictionary<string, string>>(secret);

            var connString = $"Server={parametros["host"]};Database={parametros["dbname"]};Uid={parametros["username"]};Pwd='{parametros["password"]}';CharSet=utf8;Connection Timeout=30";

            return options.UseMySql(connString, ServerVersion.AutoDetect(connString));
        }

        public static string GetConnectionString()
        {
            var secrets = new SecretsHelper();

            var secretName = GetSecretName(MySqlSecretOption.Default);
            var secret = secrets.GetSecret(secretName);

            var parametros = JsonConvert.DeserializeObject<Dictionary<string, string>>(secret);

            return $"Server={parametros["host"]};Database={parametros["dbname"]};Uid={parametros["username"]};Pwd='{parametros["password"]}';CharSet=utf8;Connection Timeout=30";
        }

        public static string GetSecretName(MySqlSecretOption secretOption)
        {
            return secretOption switch
            {
                MySqlSecretOption.Default => "DefaultDb",
                _ => throw new Exception("Invalid secret name!")
            };
        }
    }
}
