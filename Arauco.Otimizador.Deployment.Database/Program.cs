using Arauco.Otimizador.Data.MySql;
using DbUp;
using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace Arauco.Otimizador.Deployment.Database
{
    internal class Program
    {
        static int Main(string[] args)
        {
            Console.WriteLine("Database Update Process");

            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            var connString = OptionsHelper.GetConnectionString(config);

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