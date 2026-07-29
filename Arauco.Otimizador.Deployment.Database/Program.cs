using Arauco.Otimizador.Data.MySql;
using DbUp;
using Newtonsoft.Json;
using System.Reflection;
using Techer.Aws.Secrets;

namespace Arauco.Otimizador.Deployment.Database
{
    internal class Program
    {
        async static Task<int> Main(string[] args)
        {
            Console.WriteLine("Database Update Process");

            var secretsHelper = new SecretsHelper();

            var secret = secretsHelper.GetSecret(OptionsHelper.GetSecretName(MySqlSecretOption.Default));

            var parameters = JsonConvert.DeserializeObject<Dictionary<string, string>>(secret);
            var connString = $"Server={parameters["host"]};Database={parameters["dbname"]};Uid={parameters["username"]};Pwd='{parameters["password"]}';CharSet=utf8;Allow User Variables=True;";

            //Console.WriteLine(connString);

            try
            {
                var upgrader =
                    DeployChanges.To
                        .MySqlDatabase(connString)
                            .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly(), (s) => s.StartsWith("Arauco.Otimizador.Deployment.Database.Scripts"))
                            .WithTransactionPerScript()
                            .LogToConsole()
                            .WithExecutionTimeout(TimeSpan.FromSeconds(180))
                            .Build();

                var result = upgrader.PerformUpgrade();

                if (!result.Successful)
                    throw result.Error;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());

                return -1;
            }

            Console.WriteLine("Atualizacao concluida com sucesso");

            return 0;
        }
    }
}